using System;
using System.Text;

namespace UPA.MVP3.TrustEmission
{
    public static class EvidenceEncoder
    {
        public static string Encode(string runId, string auditHash)
        {
            if (runId == null) throw new ArgumentNullException(nameof(runId));
            if (auditHash == null) throw new ArgumentNullException(nameof(auditHash));

            int runIdLength = Encoding.UTF8.GetByteCount(runId);
            int auditHashLength = Encoding.UTF8.GetByteCount(auditHash);

            return $"RUNID:{runIdLength}:{runId}\nAUDIT:{auditHashLength}:{auditHash}";
        }
    }
}
