using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DxLibDLL;//DxLibを使用
using CoreTweet;//CoreTweetを使用
using System.IO;//FileInfoを使用

// デスクトップマスコットの作り方 https://qiita.com/massoumen/items/2985a0fb30472b97a590
// Dxlib質問掲示板 https://dxlib.xsrv.jp/cgi/patiobbs/patio.cgi
// Dxlibリファレンス https://dxlib.xsrv.jp/dxfunc.html
// Dxlib隠しリファレンス https://densanken.com/wiki/index.php?dx%A5%E9%A5%A4%A5%D6%A5%E9%A5%EA%B1%A3%A4%B7%B4%D8%BF%F4%A4%CE%A5%DA%A1%BC%A5%B8

namespace DesktopMascot
{
    public partial class Form1 : Form
    {
        private int modelHandle;	//3Dモデル
        private int attachIndex;	//モーション
        private float totalTime;	//モーションの総再生時間
        private float playTime;		//モーションの再生位置
        private float playSpeed;	//モーションの再生位置を進める速度
        private Tokens tokens;		//Twitterアカウントの認証

        //CPU使用率を下げるための休止処理
        static async void Delay()
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine(i);
                await Task.Delay(100);
            }
        }

        //フォームの生成
        public Form1()
        {
            InitializeComponent();//フォームの初期設定

            ClientSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);//画面サイズの設定
            Text = "DesktopMascot";//ウインドウの名前を設定
            AllowDrop = true;//ドラッグ&ドロップを許可

            tokens = Tokens.Create("ConsumerKey＊＊＊＊＊", "ConsumerSecret＊＊＊＊＊", "AccessToken＊＊＊＊＊", "AccessSecret＊＊＊＊＊");//Twitterアカウントの認証

            DX.SetOutApplicationLogValidFlag(DX.FALSE);//Log.txtを生成しないように設定
            DX.SetUserWindow(Handle);//DxLibの親ウインドウをこのフォームに設定
            DX.SetZBufferBitDepth(24);// Zバッファの深度を24bitに変更
            DX.SetCreateDrawValidGraphZBufferBitDepth(24); // 裏画面のZバッファの深度を24bitに変更　(現時点では不要だけど、いつか要る日が来る)
            DX.SetFullSceneAntiAliasingMode(4, 2); // 画面のフルスクリーンアンチエイリアスモードの設定をする
            DX.SetDrawValidMultiSample(4, 2); // (たぶん)描画対象にできるグラフィックのマルチサンプリング設定を行う
            DX.DxLib_Init();//DxLibの初期化処理
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);//描画先を裏画面に設定

            modelHandle = DX.MV1LoadModel("＊＊＊読み込みたいモデル＊＊＊");//3Dモデルの読み込み ←.pmd(モデルファイル)よりも .mv1(モーション込みモデルファイル)の方が動作が軽くなる気がする
            attachIndex = DX.MV1AttachAnim(modelHandle, 0, -1, DX.FALSE);//モーションの選択（上で.mv1ファイルを指定している場合、このモーションの選択はいらないかも）
            totalTime = DX.MV1GetAttachAnimTotalTime(modelHandle, attachIndex);//モーションの総再生時間を取得
            playTime = 0.0f;//モーションの再生位置
            playSpeed = 1.0f;//モーションの再生位置を進める速度

            DX.MV1SetPosition(modelHandle, DX.VGet(15.0f, -15.0f, 80.0f));//モデルの座標を設定
            //DX.MV1SetRotationXYZ(modelHandle, DX.VGet(0.0f, 7.5f, 0.0f));//モデルのY軸の回転値をセットする
            DX.SetCameraNearFar(0.1f, 1000.0f);//奥行0.1～1000をカメラの描画範囲とする
            DX.SetCameraPositionAndTarget_UpVecY(DX.VGet(0.0f, 10.0f, -25.0f), DX.VGet(-15.0f,15.0f, 0.0f));//第1引数の位置から第2引数の位置を見る角度にカメラを設置
        }

        public void MainLoop()
        {
            DX.ClearDrawScreen();//裏画面を消す
            DX.DrawBox(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, DX.GetColor(0, 0, 0), DX.TRUE);//背景を設定(透過させる)
            playTime += playSpeed;//時間を進める

            //モーションの再生位置が終端まで来たら最初に戻す
            if (playTime >= totalTime)
            {
                playTime = 0.0f;
            }

            DX.MV1SetAttachAnimTime(modelHandle, attachIndex, playTime);//モーションの再生位置を設定
            DX.MV1DrawModel(modelHandle);//3Dモデルの描画

            //ESCキーを押したら終了
            if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) != 0)
            {
                Close();
            }

            DX.ScreenFlip();//裏画面を表画面にコピー

            Delay();//休止処理
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DX.DxLib_End();//DxLibの終了処理
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.None;//フォームの枠を非表示にする
            TransparencyKey = Color.FromArgb(0, 0, 0);//透過色を設定
        }

        // Twitter 画像投稿機能
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされた場合受け付ける
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // Twitter 画像投稿機能
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] path = (string[])e.Data.GetData(DataFormats.FileDrop, false);//ドロップされたファイルのパスを取得(複数可)
            var ids = new List<long>();

            //各画像をアップロードしIDを取得
            foreach (var p in path)
            {
                MediaUploadResult image = tokens.Media.Upload(media: new FileInfo(p));
                ids.Add(image.MediaId);
            }

            Status s = tokens.Statuses.Update(status: "＊＊＊自動投稿時の任意の文字＊＊＊", media_ids: ids);//画像をツイート
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}