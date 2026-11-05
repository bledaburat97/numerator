using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    // Local crash-safe persistence. This is not a purchase-verification or trusted-server boundary.
    public sealed class AtomicPlayerProfileStorage : IPlayerProfileStorage
    {
        private sealed class Envelope { public string Payload; public string Checksum; }
        private readonly string _path;
        private readonly Action<string> _recovered;
        private readonly Action _beforePublish;
        public AtomicPlayerProfileStorage(string path,Action<string> recovered=null,Action beforePublish=null)
        { _path=Path.GetFullPath(path);_recovered=recovered;_beforePublish=beforePublish; }

        public PlayerProfileData Load()
        {
            bool exists=false;
            foreach(string candidate in new[]{_path,_path+".backup"})
            {
                if(!File.Exists(candidate))continue;
                exists=true;
                try
                {
                    var envelope=JsonConvert.DeserializeObject<Envelope>(File.ReadAllText(candidate));
                    if(envelope?.Payload==null || envelope.Checksum!=Hash(envelope.Payload)) throw new InvalidDataException("Profile checksum mismatch.");
                    var profile=JsonConvert.DeserializeObject<PlayerProfileData>(envelope.Payload);
                    PlayerProfileValidation.Upgrade(profile);
                    PlayerProfileValidation.Validate(profile);
                    if(candidate!=_path)_recovered?.Invoke("Player profile recovered from its last committed backup.");
                    return profile;
                }
                catch(UnsupportedProfileVersionException) { throw; }
                catch(JsonException) { }
                catch(InvalidDataException) { }
            }
            if(exists) throw new InvalidDataException("Player profile and backup could not be read. Existing files were preserved.");
            return null;
        }

        public void Save(PlayerProfileData profile)
        {
            PlayerProfileValidation.Validate(profile);
            string payload=JsonConvert.SerializeObject(profile);
            string encoded=JsonConvert.SerializeObject(new Envelope {Payload=payload,Checksum=Hash(payload)});
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            string temporary=_path+"."+Guid.NewGuid().ToString("N")+".pending";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    byte[] bytes=new UTF8Encoding(false).GetBytes(encoded);
                    stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                _beforePublish?.Invoke();
                // The old file is never deleted before the replacement is ready.
                if(File.Exists(_path)) File.Replace(temporary,_path,_path+".backup");
                else File.Move(temporary,_path);
            }
            finally { if(File.Exists(temporary)) File.Delete(temporary); }
        }

        private static string Hash(string text)
        {
            using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
