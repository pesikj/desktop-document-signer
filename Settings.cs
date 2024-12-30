using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopDocumentSigner
{
    internal class Settings
    {
        public required string CertificateHash { get; set; }
        public required string SignatureLocation { get; set; }
    }
}
