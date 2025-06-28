namespace BveExMultiPlaying.Common.Models;

//各列車インスタンス用の情報クラス
public class TrainInfoData
{
    //フィールド
    //UserID
    public string ClientId { set; get; } = "";

    //列車番号
    public string TrainNumber { set; get; } = "";

    //位置
    public double Location { set; get; } = 0;

    //速度
    public double Speed { set; get; } = 0;

    public TrainInfoData Clone()
    {
        return new()
        {
            ClientId = this.ClientId,
            TrainNumber = this.TrainNumber,
            Location = this.Location,
            Speed = this.Speed
        };
    }
}