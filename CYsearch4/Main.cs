using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace CYsearch4
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        YTManager yTManager = new YTManager();
        JArray liveArray = new JArray();

        private void SearchButton_Click(object sender, EventArgs e)
        {
            liveTree.Nodes.Clear();
            
            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
                searchButton.Text = "検索開始";
            }
            if (StreamText.Text == "") MessageBox.Show("配信名が入力されていません");
            else
            {
                searchButton.Text = "検索停止";  
                bgWorker.RunWorkerAsync();
            }
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            statusLabel.Text = "ライブ検索中";
            JArray liveResult = yTManager.SearchStream(StreamText.Text);

            statusLabel.Text = "コメント取得中";
            liveArray = yTManager.getComment(liveResult);
        }

        //終了時
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //代入
            for (int i = 0; i < liveArray.Count; i++)
            {
                liveTree.Nodes.Add(liveArray[i]["title"].ToString());
                liveTree.Nodes[i].Nodes.Add("動画ID").Nodes.Add(liveArray[i]["videoid"].ToString());
                liveTree.Nodes[i].Nodes.Add("リスナー");
                foreach (string viewName in liveArray[i]["view"])
                {
                    liveTree.Nodes[i].Nodes[1].Nodes.Add(viewName);
                }
                liveTree.Nodes[i].Expand();
            }
            statusLabel.Text = "取得完了";
            searchButton.Text = "検索開始";
        }


        private void saveButton_Click(object sender, EventArgs e)
        {
            // SaveFileDialogを表示
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "検索結果を保存する";
            saveFileDialog.Filter = "Json files (*.json)|*.json|Text files (*.txt)|*.txt";
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog.FileName = @"LiveData.json";
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                //「保存」ボタンクリック時の処理
                File.WriteAllText(saveFileDialog.FileName, liveArray.ToString());
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
