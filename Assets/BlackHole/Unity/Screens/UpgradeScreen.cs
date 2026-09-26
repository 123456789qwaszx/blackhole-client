using System;
using System.Collections.Generic;
using System.Globalization;
using BlackHole.Core;
using TMPro;
using UnityEngine;

namespace BlackHole.Unity
{
    // 업그레이드 화면(플레이어가 보는 노드 트리, F02의 임시 모양). Gold, 산 노드 수, 트리 보기를 가진다.
    // 노드 트리의 규칙을 모른다 — 표시 값은 ScreenFlow가 넘기고, 눌린 노드는 노드 ID로 알린다.
    // 트리 영역(TreeViewport)에 트리 보기(NodeTreeView)가 없으면 붙인다. 사용자가 만든 프리팹도 자식 이름만 맞으면 된다.
    public sealed class UpgradeScreen : UIRoot<UpgradeScreen.Refs>
    {
        public enum Refs
        {
            GoldText,
            ProgressText,
            TreeViewport,
        }

        public event Action<string> NodeClicked;

        private TMP_Text _gold;
        private TMP_Text _progress;
        private NodeTreeView _tree;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            _gold = View.Text(Refs.GoldText);
            _progress = View.Text(Refs.ProgressText);

            RectTransform viewport = View.Rect(Refs.TreeViewport);

            if (viewport != null)
            {
                _tree = viewport.GetComponent<NodeTreeView>();

                if (_tree == null)
                    _tree = viewport.gameObject.AddComponent<NodeTreeView>();

                _tree.NodeClicked += id => NodeClicked?.Invoke(id);
            }
        }

        public void BuildTree(IReadOnlyList<NodeTreeView.NodeItem> nodes, IReadOnlyList<(string A, string B)> links) =>
            _tree?.Build(nodes, links);

        public void ShowNodes(Func<string, NodeState> stateOf) => _tree?.Show(stateOf);

        public void ShowGold(long gold)
        {
            if (_gold != null)
                _gold.text = "Gold " + gold.ToString("N0", CultureInfo.InvariantCulture);
        }

        public void ShowProgress(int owned, int total)
        {
            if (_progress != null)
                _progress.text = $"{owned} / {total} nodes";
        }
    }
}
