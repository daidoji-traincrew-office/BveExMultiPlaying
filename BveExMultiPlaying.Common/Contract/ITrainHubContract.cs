using BveExMultiPlaying.Common.Models;

namespace BveExMultiPlaying.Common.Contract;

public interface ITrainHubContract
{
    Task SendTrainData(TrainInfoData trainInfoData);
}

public interface ITrainHubClientContract
{
    Task ReceiveTrainData(TrainInfoData trainInfoData);
}