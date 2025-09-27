using SocketIOClient;
using System.Threading.Tasks;
using UnityEngine;
public class CreateRoomAck
{
    public bool ok { get; set; }
    public string roomId { get; set; }
    public string error { get; set; }
}
public static class RoomSocket
{
    public static async Task<CreateRoomAck> CreateRoomAsync(SocketIO socket, int capacity = 8)
    {
        var tcs = new TaskCompletionSource<CreateRoomAck>();

        await socket.EmitAsync("create-room", response =>
        {
            try
            {
                var ack = response.GetValue<CreateRoomAck>(); // parse the single JSON ack object
                tcs.TrySetResult(ack);
            }
            catch (System.Exception ex)
            {
                tcs.TrySetResult(new CreateRoomAck { ok = false, error = ex.Message });
            }
        });

        // simple 5s timeout:
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        if (finished != tcs.Task)
            return new CreateRoomAck { ok = false, error = "create-room ack timed out" };

        return await tcs.Task;
    }
}
