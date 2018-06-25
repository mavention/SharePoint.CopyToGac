using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavention.VisualStudio.SharePoint.CopyToGac {
    public class ThreadSafeStreamReader {
        StreamReader streamReader = null;
        string _Text = null;
        public string Text {
            get {
                return _Text;
            }
        }

        public ThreadSafeStreamReader(StreamReader sr) {
            streamReader = sr;
        }

        public void Go() {
            _Text = streamReader.ReadToEnd();
        }
    }
}
