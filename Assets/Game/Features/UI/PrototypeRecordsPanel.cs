using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    public static class PrototypeRecordsPanel
    {
        public static void OpenJournal(Transform owner)
        {
            var panel = PrototypeUi.Modal(owner, "모험 기록 · 게임은 계속 진행됩니다");
            var entries = new List<string>(PrototypeSave.Progress.memories);
            foreach (var entry in PrototypeSession.Journal) if (!entries.Contains(entry)) entries.Add(entry);
            var body = PrototypeUi.Text(panel, "Journal", "", .05f,.17f,.9f,.57f,23);
            var pageLabel = PrototypeUi.Text(panel,"Page","",.4f,.76f,.2f,.07f,20); var page = 0;
            void Refresh()
            {
                var count = Mathf.Max(1, Mathf.CeilToInt(entries.Count / 3f)); page = Mathf.Clamp(page, 0, count - 1);
                var text = $"팀 누적 {PrototypeSession.RunScore}점 · {PrototypeSession.RunSeconds:0.0}초\n\n";
                if (entries.Count == 0) text += "아직 기억이 없습니다. 목걸이를 켜고 빛나는 기억 가까이 다가가세요.";
                for (var i = page * 3; i < Mathf.Min(entries.Count, page * 3 + 3); i++) text += entries[i] + "\n\n";
                body.text = text; pageLabel.text = $"{page + 1} / {count}";
            }
            PrototypeUi.Button(panel,"Previous","이전",.06f,.85f,.23f,.09f,()=>{page--;Refresh();});
            PrototypeUi.Button(panel,"Close","닫기",.38f,.85f,.24f,.09f,PrototypeUi.CloseModal);
            PrototypeUi.Button(panel,"Next","다음",.71f,.85f,.23f,.09f,()=>{page++;Refresh();});
            Refresh();
        }
        public static void OpenRanking(Transform owner)
        {
            var panel = PrototypeUi.Modal(owner,"로컬 팀 기록 · 공개 랭킹 아님"); var chapter = 1;
            var title = PrototypeUi.Text(panel,"Filter","",.05f,.15f,.9f,.12f,21,PrototypeUi.Accent);
            var body = PrototypeUi.Text(panel,"Ranking","",.05f,.31f,.9f,.45f,22);
            void Refresh()
            {
                var records = PrototypeSave.Ranking(chapter, PrototypeTuning.Current.rulesVersion);
                title.text = $"챕터 {chapter} · 기록 시작 4인 · 배점 {PrototypeTuning.Current.rulesVersion}\n다른 배점 버전·다른 챕터는 이 순위표에 섞지 않습니다.";
                var text = new StringBuilder("순위      팀 총점      시간\n");
                for (var i=0; i<Mathf.Min(8,records.Count); i++) text.AppendLine($"{i+1,2}          {records[i].score,6}        {records[i].seconds:0.0}초");
                if (records.Count == 0) text.Append("아직 이 조건의 클리어 기록이 없습니다.");
                body.text = text.ToString();
            }
            PrototypeUi.Button(panel,"PreviousChapter","이전 챕터",.05f,.85f,.25f,.09f,()=>{chapter=chapter==1?7:chapter-1;Refresh();});
            PrototypeUi.Button(panel,"Close","닫기",.37f,.85f,.26f,.09f,PrototypeUi.CloseModal);
            PrototypeUi.Button(panel,"NextChapter","다음 챕터",.7f,.85f,.25f,.09f,()=>{chapter=chapter==7?1:chapter+1;Refresh();});
            Refresh();
        }
    }
}
