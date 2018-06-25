using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.VisualStudio.SharePoint.Commands;

namespace Mavention.VisualStudio.SharePoint.CopyToGac.Commands {
    public class CopyToGacCommands {
        [SharePointCommand(CommandIds.GetApplicationPoolName)]
        private string GetApplicationPoolName(ISharePointCommandContext context, string url) {
            string applicationPoolName = null;

            try {
                using (SPSite site = new SPSite(url)) {
                    applicationPoolName = site.WebApplication.ApplicationPool.Name;
                }
            }
            catch {
            }

            return applicationPoolName;
        }
    }
}
