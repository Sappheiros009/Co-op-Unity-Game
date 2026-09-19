using System;
using System.Globalization;

namespace SlimeCoop.Prototype
{
    /// <summary>Menu input only. Never selects a public address or grants gameplay authority.</summary>
    public sealed class PrototypeLocalConnection
    {
        public string Name { get; }
        public ushort Port { get; }
        public int Capacity { get; }
        private PrototypeLocalConnection(string name, ushort port, int capacity)
        { Name=name; Port=port; Capacity=capacity; }

        public static bool TryCreate(string name, string port, int capacity, out PrototypeLocalConnection request, out string error)
        {
            request=null; name=(name??"").Trim(); error="";
            if(name.Length==0 || name.Length>24) error="이름은 1~24자로 입력하세요.";
            else foreach(var c in name) if(char.IsControl(c) || c=='<' || c=='>') { error="이름에 줄바꿈이나 < > 기호를 사용할 수 없습니다."; break; }
            if(error.Length>0) return false;
            if(!ushort.TryParse((port??"").Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out var parsed) || parsed<1024)
            { error="포트는 1024~65535 사이의 숫자입니다. 참가자는 같은 포트를 사용하세요."; return false; }
            if(capacity<2 || capacity>4) { error="방 정원은 2~4명입니다."; return false; }
            request=new PrototypeLocalConnection(name,parsed,capacity); return true;
        }

        public static string ExplainFailure(string reason)
        {
            reason=reason??"";
            if(reason.Contains("server_instance_mismatch")) return "이 포트에는 다른 서버가 있습니다. 다른 포트로 만들거나 기존 방에 참가하세요.";
            if(reason.Contains("version_mismatch")) return "서버와 게임 버전이 다릅니다. 같은 최신 빌드로 실행하세요.";
            if(reason.Contains("room_full")) return "방이 가득 찼습니다. 빈자리가 생긴 뒤 다시 참가하세요.";
            if(reason.Contains("run_locked")) return "이 방은 모험 중입니다. 대기방으로 복귀한 뒤 참가하세요.";
            if(reason.Contains("invalid_name")) return "이름을 확인하고 다시 참가하세요.";
            return "서버에 연결하지 못했습니다. 같은 PC에서 서버가 켜져 있는지와 포트를 확인하세요.";
        }
    }
}
