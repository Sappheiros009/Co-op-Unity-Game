using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Owns a process handle, NOT server lifetime. Leaving a client must not kill the team's server.</summary>
    public sealed class PrototypeLocalServerProcess : IDisposable
    {
        private Process _process;
        public string InstanceId { get; private set; }
        public string LogPath { get; private set; }
        public string ReadyPath { get; private set; }
        public int ProcessId { get; private set; }
        public bool HasExited => _process==null || _process.HasExited;
        public bool IsReady
        {
            get
            {
                try { return File.Exists(ReadyPath) && File.ReadAllText(ReadyPath).Trim()==InstanceId; }
                catch(IOException) { return false; } // The server may still be writing its one-time receipt.
            }
        }
        public static bool TryStart(PrototypeLocalConnection request, out PrototypeLocalServerProcess server, out string error)
        {
            server=null; error="";
            if(Application.platform!=RuntimePlatform.WindowsPlayer && Application.platform!=RuntimePlatform.WindowsEditor)
            { error="이 로컬 전용 서버 도우미는 Windows 빌드용입니다."; return false; }
            try
            {
                var exe=Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath,"..","Build","FullPrototype","SlimeCoopPrototype.exe"))
                    : Application.dataPath.Substring(0,Application.dataPath.Length-"_Data".Length)+".exe";
                if(!File.Exists(exe)) { error="Windows 프로토타입을 먼저 빌드하세요. 편집기와 서버는 같은 빌드여야 합니다."; return false; }
                var id=Guid.NewGuid().ToString("N");
                var root=Path.Combine(PrototypeSave.RootOverride??Application.persistentDataPath,"LocalNetwork",id);
                Directory.CreateDirectory(root);
                var launched=new PrototypeLocalServerProcess { InstanceId=id, LogPath=Path.Combine(root,"server.log"),ReadyPath=Path.Combine(root,"ready.txt") };
                // Only validated numbers, generated identities and quoted local paths enter the command line.
                var args=$"-batchmode -nographics --slime-network server --network-port {request.Port} --network-capacity {request.Capacity} --network-server-id {id} --network-startup-timeout 30 --network-ready-file \"{launched.ReadyPath}\" --network-save-root \"{Path.Combine(root,"Save")}\" -logFile \"{launched.LogPath}\"";
                launched._process=Process.Start(new ProcessStartInfo(exe,args)
                { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=Path.GetDirectoryName(exe) });
                if(launched._process==null) { error="전용 서버 프로세스를 시작하지 못했습니다."; return false; }
                launched.ProcessId=launched._process.Id; server=launched; return true;
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            { error="전용 서버를 실행하지 못했습니다. 실행 파일과 저장 폴더 권한을 확인하세요. ("+ex.GetType().Name+")"; return false; }
        }
        public void Dispose() { _process?.Dispose(); _process=null; }
    }
}
