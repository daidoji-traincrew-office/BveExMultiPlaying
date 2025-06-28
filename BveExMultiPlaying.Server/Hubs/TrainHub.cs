using BveExMultiPlaying.Common.Contract;
using BveExMultiPlaying.Common.Models;
using Microsoft.AspNetCore.SignalR;

namespace BveExMultiPlaying.Server.Hubs;

public class TrainHub: Hub<ITrainHubClientContract>, ITrainHubContract
{
    public async Task SendTrainData(TrainInfoData trainInfoData)
    {
        // 自分以外のクライアントに列車データを送信
        await Clients.AllExcept(Context.ConnectionId).ReceiveTrainData(trainInfoData);
    }
}