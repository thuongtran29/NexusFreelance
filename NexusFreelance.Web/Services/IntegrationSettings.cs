using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;
using System.Web.Script.Serialization;

namespace NexusFreelance.Web.Services
{
    // Environment wins. Optional local secrets are DPAPI-encrypted outside the web root.
    public static class IntegrationSettings
    {
        private static readonly object Gate=new object();
        private static readonly byte[] Entropy=Encoding.UTF8.GetBytes("NexusFreelance.integrations.v1");
        private static Dictionary<string,string> cached=new Dictionary<string,string>();
        private static DateTime stamp; private static long length=-1;
        private static readonly HashSet<string> Allowed=new HashSet<string>{"NEXUS_AI_ENABLED","NEXUS_AI_PROVIDER","NEXUS_GEMINI_API_KEY","NEXUS_GEMINI_MODEL","NEXUS_OPENAI_API_KEY","NEXUS_OPENAI_MODEL","NEXUS_GOOGLE_CLIENT_ID","NEXUS_GOOGLE_CLIENT_SECRET","NEXUS_GOOGLE_REDIRECT_URI","NEXUS_MAIL_MODE","NEXUS_MAIL_PICKUP_DIRECTORY","NEXUS_PUBLIC_BASE_URL","NEXUS_SMTP_HOST","NEXUS_SMTP_PORT","NEXUS_SMTP_FROM","NEXUS_SMTP_USERNAME","NEXUS_SMTP_PASSWORD"};
        public static string Get(string key)
        {
            var value=Environment.GetEnvironmentVariable(key);
            if(!string.IsNullOrWhiteSpace(value))return value;
            if(!Allowed.Contains(key))return "";
            var web=HostingEnvironment.ApplicationPhysicalPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var path=Path.GetFullPath(Path.Combine(web,"..","_local","integrations.dat"));
            lock(Gate)
            {
                try {
                    var info=new FileInfo(path);
                    var nextStamp=info.Exists?info.LastWriteTimeUtc:DateTime.MinValue;
                    var nextLength=info.Exists?info.Length:0;
                    if(stamp!=nextStamp || length!=nextLength){cached=ReadProtected(path);stamp=nextStamp;length=nextLength;}
                    return cached.TryGetValue(key,out value)?value??"":"";
                }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException || ex is ArgumentException || ex is InvalidOperationException)
                { cached=new Dictionary<string,string>();length=-1;System.Diagnostics.Trace.TraceWarning("Local integration settings could not be read; provider remains unavailable.");return ""; }
            }
        }
        public static Dictionary<string,string> ReadProtected(string path)
        {
            if(!File.Exists(path))return new Dictionary<string,string>();
            if(new FileInfo(path).Length>65536)throw new InvalidOperationException("Integration settings file is too large.");
            var bytes=File.ReadAllBytes(path);
            var clear=ProtectedData.Unprotect(bytes,Entropy,DataProtectionScope.CurrentUser);
            try {
                var values=new JavaScriptSerializer{MaxJsonLength=65536}.Deserialize<Dictionary<string,string>>(Encoding.UTF8.GetString(clear));
                if(values==null)throw new InvalidOperationException("Invalid integration settings.");
                return values;
            } finally {Array.Clear(clear,0,clear.Length);}
        }
    }
}
