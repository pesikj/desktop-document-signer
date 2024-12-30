using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopDocumentSigner
{
    internal class SignatureResult
    {
        public required string ResultText;
        public string? SignedDocumentBase64;
    }
}
