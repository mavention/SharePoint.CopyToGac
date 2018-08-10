using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.SharePoint;

namespace Mavention.VisualStudio.SharePoint.CopyToGac {
    [Export(typeof(ISharePointProjectExtension))]
    public class CopyToGacProjectExtension : ISharePointProjectExtension {
        public void Initialize(ISharePointProjectService projectService) {
            projectService.ProjectMenuItemsRequested += projectService_ProjectMenuItemsRequested;
        }

        void projectService_ProjectMenuItemsRequested(object sender, SharePointProjectMenuItemsRequestedEventArgs e) {
            IMenuItem buildAndDeployMenuItem = e.ActionMenuItems.Add("Quick Deploy");
            buildAndDeployMenuItem.Click += buildAndDeployMenuItem_Click;
            buildAndDeployMenuItem.IsEnabled = e.Project.AssemblyDeploymentTarget == AssemblyDeploymentTarget.GlobalAssemblyCache &&
                e.Project.IncludeAssemblyInPackage &&
                e.Project.IsSandboxedSolution == false;
        }

        void buildAndDeployMenuItem_Click(object sender, MenuItemEventArgs e) {
            ISharePointProject project = e.Owner as ISharePointProject;
            ISharePointProjectLogger logger = project.ProjectService.Logger;
            logger.ActivateOutputWindow();

            if (project.Package.BuildPackage()) {
                ProcessStartInfo gacutilStartInfo = new ProcessStartInfo(@"c:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\gacutil.exe", String.Format("/i {0}", project.OutputFullPath)) {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                Process gacutil = Process.Start(gacutilStartInfo);

                ThreadSafeStreamReader stdoutStream = new ThreadSafeStreamReader(gacutil.StandardOutput);
                ThreadSafeStreamReader stderrStream = new ThreadSafeStreamReader(gacutil.StandardError);

                Thread stdoutThread = new Thread(stdoutStream.Go);
                Thread stderrThread = new Thread(stdoutStream.Go);

                stdoutThread.Start();
                stderrThread.Start();

                stdoutThread.Join();
                stderrThread.Join();

                gacutil.WaitForExit();

                if (String.IsNullOrEmpty(stderrStream.Text)) {
                    logger.WriteLine(stdoutStream.Text, LogCategory.Message);

                    logger.WriteLine("Recycling Application Pool...", LogCategory.Status);

                    string applicationPoolName = project.ProjectService.SharePointConnection.ExecuteCommand<string, string>(Commands.CommandIds.GetApplicationPoolName, project.SiteUrl.ToString());
                    if (!String.IsNullOrEmpty(applicationPoolName)) {
                        using (DirectoryEntry appPool = new DirectoryEntry(String.Format("IIS://localhost/w3svc/apppools/{0}", applicationPoolName))) {
                            appPool.Invoke("Stop", null);
                            appPool.Invoke("Start", null);
                        }

                        logger.WriteLine("Application Pool successfully recycled", LogCategory.Status);
                    }
                    else {
                        logger.WriteLine("Application Pool not found", LogCategory.Error);
                    }
                }
                else {
                    logger.WriteLine(stderrStream.Text, LogCategory.Error);
                    logger.WriteLine("Installing assembly failed", LogCategory.Status);
                }
            }
            else {
                logger.WriteLine("Building Solution Package Failed", LogCategory.Error);
            }
        }
    }
}
