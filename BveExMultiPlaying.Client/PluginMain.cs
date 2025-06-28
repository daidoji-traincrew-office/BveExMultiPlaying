using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BveEx.Extensions.MapStatements;
using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;
using BveExMultiPlaying.Common.Contract;
using BveTypes.ClassWrappers;
using BveExMultiPlaying.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace BveExMultiPlaying.Client
{
    [Plugin(PluginType.MapPlugin)]
    public class PluginMain : AssemblyPluginBase, ITrainHubClientContract
    {
        // サーバーURL
        private const string ServerUrl = "https://localhost:7261/hubs/train";

        //BveEX自列車番号設定オリジナルマップ構文取得用
        private readonly IStatementSet Statements;

        //デバッグテキスト表示用
        private AssistantText debugText;

        private readonly HubConnection hubConnection;

        private readonly ITrainHubContract hubContract;

        //自列車情報（送信用）
        TrainInfoData myTrain = new TrainInfoData(); //自列車情報用インスタンスを生成

        private Timer sendTimer;

        //他列車情報（受信用）
        private static Dictionary<string, TrainInfoData> OtherTrainData { get; set; }

        private readonly object trainMapLockObj = new object(); //他列車情報のスレッドセーフ用ロックオブジェクト

        //先行列車解除の有無用
        //private Train Train;
        //private PreTrainPatch PreTrainPatch;

        //先行列車解除の有無用クラス
        /*private class PreTrainLocationConverter : IPreTrainLocationConverter
        {
            private readonly Train SourceTrain;
            private readonly SectionManager SectionManager;

            public PreTrainLocationConverter(Train sourceTrain, SectionManager sectionManager)
            {
                SourceTrain = sourceTrain;
                SectionManager = sectionManager;
            }

            public PreTrainLocation Convert(PreTrainLocation source)
                => SourceTrain.TrainInfo.TrackKey == "0" ? PreTrainLocation.FromLocation(SourceTrain.Location, SectionManager) : source;
        }*/


        //コンストラクタ
        public PluginMain(PluginBuilder builder, Timer sendTimer) : base(builder)
        {
            this.sendTimer = sendTimer;
            //BveEX自列車番号設定オリジナルマップ構文取得用
            Statements = Extensions.GetExtension<IStatementSet>();
            //デバッグテキスト表示用
            debugText = AssistantText.Create("");
            BveHacker.Assistants.Items.Add(debugText);
            //自列車情報（送信用）
            myTrain.ClientId = Guid.NewGuid().ToString(); //ユーザーIDを発行、自列車情報用インスタンスmyTrainに設定
            //sendTimer = new Timer(SendDataToServer, null, 1000, 1000);//1秒ごとにデータ送信
            //他列車情報（受信用）
            OtherTrainData = new Dictionary<string, TrainInfoData>();
            //receiveTimer = new Timer(ReceiveOtherClientsData, null, 1000, 1000);//1秒ごとにデータ受信//Tickに移動
            //SignalRハブ接続設定
            hubConnection = new HubConnectionBuilder()
                .WithUrl(ServerUrl) // SignalRハブのURL
                .WithAutomaticReconnect()
                .Build();
            hubConnection.On(
                "ReceiveTrainData", 
                async (TrainInfoData trainInfoData) => await ReceiveTrainData(trainInfoData));
            //イベント購読
            BveHacker.ScenarioCreated += OnScenarioCreated;
        }

        //終了時処理
        public override void Dispose()
        {
            BveHacker.Assistants.Items.Remove(debugText);
            sendTimer?.Dispose();
            hubConnection?.StopAsync().Wait();
            hubConnection?.DisposeAsync().AsTask().Wait();
            BveHacker.ScenarioCreated -= OnScenarioCreated;
        }

        //フレーム毎処理
        public override void Tick(TimeSpan elapsed)
        {
            ApplyReceivedData(elapsed);
            foreach (var trains in BveHacker.Scenario.Trains)
            {
                
                if (OtherTrainData.TryGetValue(trains.Key, out var otherTrainData))
                {
                    continue; // 他列車情報がない、または列車が無効な場合はスキップ
                }

                trains.Value.Location = otherTrainData.Location;
                trains.Value.Speed = otherTrainData.Speed;
            }

            //デバッグテキスト表示用
            debugText.Text = "自列車番号: " + myTrain.TrainNumber +
                             $"位置: {myTrain.Location:F1}m, 速度: {myTrain.Speed:F1}m/s";

            /*if (BveHacker.Scenario.Trains.ContainsKey("0523m"))
            {
                try
                {
                debugText.Text = "自列車番号: " + myTrain.TrainNumber +
                                    $"位置: {myTrain.Location:F1}m, 速度: {myTrain.Speed:F1}m/s" + Environment.NewLine +
                                 //"他列車番号: " + "0523m" + $" :{BveHacker.Scenario.Trains["0523m"].IsEnabled} " +
                                 //$"位置: {BveHacker.Scenario.Trains["0523m"].Location:F1}m, 速度: {BveHacker.Scenario.Trains["0523m"].Speed:F1}m/s";
                    $"他列車番号: {OtherTrainData["0523m"].TrainNumber} :{BveHacker.Scenario.Trains["0523m"].IsEnabled} ,受信他列車:{OtherTrainData.Count} ,0523m受信:{OtherTrainData.ContainsKey("0523m")}" +
                    $"位置: {OtherTrainData["0523m"].Location:F1}m, 速度: {OtherTrainData["0523m"].Speed:F1}m/s";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"エラー: {ex.Message}");
                }
            }
            if (BveHacker.Scenario.Trains.ContainsKey("0521m"))
            {
                try
                {
                debugText.Text = "自列車番号: " + myTrain.TrainNumber +
                                    $"位置: {myTrain.Location:F1}m, 速度: {myTrain.Speed:F1}m/s" + Environment.NewLine +
                                 //"他列車番号: " + "0521m" + $" :{BveHacker.Scenario.Trains["0521m"].IsEnabled} " +
                                    //$"位置: {BveHacker.Scenario.Trains["0521m"].Location:F1}m, 速度: {BveHacker.Scenario.Trains["0521m"].Speed:F1}m/s";
                    $"他列車番号: {OtherTrainData["0521m"].TrainNumber} :{BveHacker.Scenario.Trains["0521m"].IsEnabled} ,受信他列車:{OtherTrainData.Count} ,0521m受信:{OtherTrainData.ContainsKey("0521m")}" +
                    $"位置: {OtherTrainData["0521m"].Location:F1}m, 速度: {OtherTrainData["0521m"].Speed:F1}m/s";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"エラー: {ex.Message}");
                }
            }*/
        }

        //シナリオ作成イベント購読時の処理
        private void OnScenarioCreated(ScenarioCreatedEventArgs e)
        {
            //0.1秒ごとにデータ送信
            sendTimer = new Timer(_ => SendDataToServer().Wait(), null, 0, 100);
            //BveEX自列車番号設定オリジナルマップ構文取得用
            Statement put = Statements.FindUserStatement("YUtrain",
                ClauseFilter.Element("MultiPlaying", 0),
                ClauseFilter.Function("TrainNumber", 1));
            MapStatementClause function = put.Source.Clauses[put.Source.Clauses.Count - 1];
            myTrain.TrainNumber = function.Args[0] as string; //自列車情報用インスタンスmyTrainに列車番号を設定
            //SignalRハブ接続開始
            hubConnection.StartAsync().Wait();
        }

        //自列車情報（送信用）イベント（1秒ごと）←次ここから書く（Location,Speedに関してはまずは1秒おき）毎フレームリスト化しない！
        private async Task SendDataToServer()
        {
            //自列車情報（位置,速度）を取得、自列車情報用インスタンスmyTrainに設定
            myTrain.Location = BveHacker.Scenario.VehicleLocation.Location;
            myTrain.Speed = BveHacker.Scenario.VehicleLocation.Speed;
            //各自列車情報をリスト化

            var clientData = myTrain.Clone();
            //自列車情報をサーバーに送信
            try
            {
                await hubContract.SendTrainData(clientData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"データ送信エラー: {ex.Message}");
            }
        }

        //他列車情報（受信用）イベント（1秒ごと）
        public Task ReceiveTrainData(TrainInfoData trainInfoData)
        {
            lock (trainMapLockObj)
            {
                OtherTrainData[trainInfoData.TrainNumber] = trainInfoData;
            }

            return Task.CompletedTask;
        }

        private void ApplyReceivedData(TimeSpan elapsed)
        {
            lock (trainMapLockObj)
            {
                foreach (var otherTrainData in OtherTrainData)
                {
                    //var trainNumber = otherTrainData.Key;
                    otherTrainData.Value.Location += otherTrainData.Value.Speed * elapsed.TotalSeconds;
                }
            }
        }
    }
}