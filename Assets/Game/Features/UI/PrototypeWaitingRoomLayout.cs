using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Shared temporary room geometry. Network rooms never create simulated party members.</summary>
    public static class PrototypeWaitingRoomLayout
    {
        public static Vector3 NetworkSpawn(int slot) => new Vector3(-4.5f + slot * 3, .12f, -5.5f);
        public static Vector3 ChapterPosition(int chapter) => new Vector3(-9 + (chapter - 1) * 3, 0, 7.2f);
        public static readonly Vector3 ReadyPosition = new Vector3(0, 0, 1.8f);

        public static Transform Build(Transform owner, bool network = false)
        {
            var existing = owner.Find("WaitingRoom_Walkable3D");
            if (existing != null) return existing;
            var root = new GameObject("WaitingRoom_Walkable3D").transform; root.SetParent(owner, false);
            var floor = PrototypeVisuals.CreateMaterial("Waiting Floor", new Color(.16f,.23f,.28f));
            var wall = PrototypeVisuals.CreateMaterial("Waiting Walls", new Color(.19f,.28f,.34f));
            PrototypeVisuals.CreateCube("Waiting Floor",root,new Vector3(0,-.5f,0),new Vector3(26,1,20),floor);
            PrototypeVisuals.CreateCube("Waiting North Wall",root,new Vector3(0,2,9.8f),new Vector3(26,4,.4f),wall);
            PrototypeVisuals.CreateCube("Waiting South Wall",root,new Vector3(0,2,-9.8f),new Vector3(26,4,.4f),wall);
            PrototypeVisuals.CreateCube("Waiting West Wall",root,new Vector3(-12.8f,2,0),new Vector3(.4f,4,20),wall);
            PrototypeVisuals.CreateCube("Waiting East Wall",root,new Vector3(12.8f,2,0),new Vector3(.4f,4,20),wall);
            var labels = new[]{"01\n광산","02\nLAVA","03\n오염 지대","04\n뇌전의 바다","05\nSquare","06\nFrozenMountain","07\nHometown"};
            for (var i=0;i<7;i++) AddStation(root,"Chapter"+(i+1),PrototypeWaitingStationKind.Chapter,labels[i],ChapterPosition(i+1),0,PrototypeChapterCatalog.Get(i+1).AccentColor,i+1);
            AddStation(root,"ReadyStation",PrototypeWaitingStationKind.Ready,"준비 장치\n선택한 챕터 출발",ReadyPosition,0,new Color(.3f,.9f,.65f));
            var kinds = new[]{PrototypeWaitingStationKind.Party,PrototypeWaitingStationKind.Specialty,PrototypeWaitingStationKind.TrialMode,PrototypeWaitingStationKind.Practice,
                PrototypeWaitingStationKind.Records,PrototypeWaitingStationKind.Memories,PrototypeWaitingStationKind.Intro,PrototypeWaitingStationKind.Back};
            var names = new[]{"인원 변경\n2 → 3 → 4","특기 변경","전체 시험\n해금 전환","조작·협동 연습","로컬 팀 기록","발견한 기억","이야기 도입","2D 로비로"};
            // The first four devices change the local simulation only. Do not expose them as online controls.
            for (var i=network?4:0;i<8;i++) AddStation(root,kinds[i]+"Station",kinds[i],names[i],new Vector3(i<4?-10.3f:10.3f,0,-6.2f+(i%4)*3.1f),i<4?90:-90,new Color(.35f,.65f,.9f));
            if (!network) for (var i=1;i<4;i++)
            {
                var obj = new GameObject("Waiting Slime "+(i+1)); obj.transform.SetParent(root,false);
                obj.transform.position = new Vector3(i==1?-3:i==2?3:5,0,-1.3f-(i==3?2:0));
                if (!Application.isPlaying) PrototypeVisuals.CreateSphere("Editor Slime Preview",obj.transform,obj.transform.position+Vector3.up*.7f,Vector3.one,
                    PrototypeVisuals.CreateMaterial("WaitingPreview"+i,Color.HSVToRGB(.45f+i*.12f,.5f,1)));
                obj.transform.rotation = Quaternion.Euler(0,180,0);
            }
            PrototypeVisuals.CreateDirectionalLight(root);
            if (!Application.isPlaying && !network)
            {
                var camera = PrototypeVisuals.CreateCamera("Waiting Room Preview Camera",new Vector3(0,1.67f,-5.5f),new Vector3(0,1.67f,7));
                camera.transform.SetParent(owner,true);
            }
            ApplyLighting(); return root;
        }
        public static void ApplyLighting()
        { RenderSettings.ambientLight = new Color(.42f,.48f,.53f); RenderSettings.fog = false; }
        private static void AddStation(Transform root,string name,PrototypeWaitingStationKind kind,string label,Vector3 position,float yaw,Color color,int chapter=0)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root,false); obj.transform.position=position; obj.transform.rotation=Quaternion.Euler(0,yaw,0);
            var material = PrototypeVisuals.CreateMaterial("WaitingStation_"+name,color,true);
            PrototypeVisuals.CreateCube("StationDevice",obj.transform,position+Vector3.up*.55f,new Vector3(1.25f,1.1f,.8f),material);
            PrototypeVisuals.CreateCube("StationFloorMarker",obj.transform,position+Vector3.up*.025f,new Vector3(2.4f,.05f,2.1f),material,false);
            var station=obj.AddComponent<PrototypeWaitingRoomStation>(); station.Kind=kind; station.Chapter=chapter; station.Label=label; station.EnsureLabel();
        }
    }
}
