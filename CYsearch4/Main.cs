using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CYsearch4
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        YoutubeAPI ytAPI = new YoutubeAPI();
        JArray viewerArray = new JArray();

        private void SearchButton_Click(object sender, EventArgs e)
        {
            liveTree.Nodes.Clear();

            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
                searchButton.Text = "検索開始";
            }
            if (StreamText.Text == "")
            {
                MessageBox.Show("配信名が入力されていません");
            }
            else
            {
                searchButton.Text = "検索停止";
                bgWorker.RunWorkerAsync();
            }
        }

        private async void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            statusLabel.Text = "ライブ検索中";
            JArray liveResult = await ytAPI.SearchStreamAsync(StreamText.Text);

            statusLabel.Text = "コメント取得中";
            viewerArray = await ytAPI.GetCommentsAsync(liveResult);

            // 非同期処理が完了するまで待機
            while (!bgWorker.CancellationPending && bgWorker.IsBusy && (viewerArray == null || viewerArray.Count == 0))
            {
                await Task.Delay(100);
            }
            bgWorker_Completed();
        }

        private void bgWorker_Completed()
        {
            statusLabel.Text = "取得完了";

            foreach (JObject item in viewerArray)
            {
                Console.WriteLine(item.ToString());
                TreeNode liveNode = new TreeNode(item["title"].ToString());
                liveNode.Nodes.Add("動画ID").Nodes.Add(item["videoid"].ToString());
                liveNode.Nodes.Add("リスナー");

                foreach (string viewName in item["viewer"])
                {
                    liveNode.Nodes[1].Nodes.Add(viewName);
                }

                liveTree.Invoke(new Action(() =>
                {
                    liveTree.Nodes.Add(liveNode);
                    liveNode.Expand();
                    searchButton.Text = "検索開始";
                }));
            }
        }


        private void saveButton_Click(object sender, EventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "検索結果を保存する";
            saveFileDialog.Filter = "Json files (*.json)|*.json|Text files (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog.FileName = @"Result.json";
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, viewerArray.ToString());
            }
        }

        private void liveTree_DoubleClick(object sender, EventArgs e)
        {

            if (liveTree.SelectedNode != null)
            {
                TreeNode selectNode = liveTree.SelectedNode;
                if (selectNode.Parent != null)
                {
                    while (selectNode.Level > 0) selectNode = selectNode.Parent;
                }
                System.Diagnostics.Process.Start($"https://youtu.be/{selectNode.Nodes[0].Nodes[0].Text}");
            }
        }

    }
}
