using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Local test profiles only, not account authentication. A live lease prevents concurrent file writers.</summary>
    public sealed class PrototypeLocalProfile : IDisposable
    {
        private FileStream _lease;
        private static PrototypeLocalProfile _active;
        public string Name { get; private set; }
        public string Root { get; private set; }
        public string BaseRoot { get; private set; }
        public static string ActiveName => _active?.Name??"";
        static PrototypeLocalProfile() { Application.quitting+=Release; }

        public static bool TryAcquire(string baseRoot, string name, out PrototypeLocalProfile profile, out string error)
        {
            profile=null;
            if(!PrototypeLocalConnection.TryCreate(name,"7797",4,out var validated,out error)) return false;
            using var hash=SHA256.Create(); var key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(validated.Name))).Replace("-","").ToLowerInvariant();
            var root=Path.Combine(Path.GetFullPath(baseRoot),"LocalPlayers",key);
            try
            {
                Directory.CreateDirectory(root);
                var lease=new FileStream(Path.Combine(root,"session.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                profile=new PrototypeLocalProfile {Name=validated.Name,Root=root,BaseRoot=baseRoot,_lease=lease}; error=""; return true;
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            { error="이 이름의 시험 저장을 다른 창에서 사용 중이거나 저장 권한이 없습니다. 다른 이름을 입력하세요."; return false; }
        }
        public static bool Activate(string name, out string error)
        {
            if(_active!=null && PrototypeSave.Root!=_active.Root) Release();
            if(_active!=null && _active.Name==name.Trim()) { error=""; return true; }
            if(PrototypeSave.HasPendingProgress) { error="현재 진행이 아직 저장되지 않았습니다. 설정에서 저장을 재시도한 뒤 이름을 변경하세요."; return false; }
            if(!TryAcquire(_active?.BaseRoot??PrototypeSave.Root,name,out var next,out error)) return false;
            Release(); _active=next; PrototypeSave.RootOverride=next.Root;
            return true;
        }
        // Keep the selected profile active in the returned 2D lobby, including any unsaved-progress retry UI.
        public static void Release() { _active?.Dispose(); _active=null; }
        public void Dispose() { _lease?.Dispose(); _lease=null; }
    }
}
