using Microsoft.AspNetCore.SignalR;

namespace testweb.Hubs // 👈 檢查這裡！大小寫要完全一致
{
    public class OrderHub : Hub
    {
        public async Task JoinOrderGroup(string orderNumber)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderNumber);
        }
        // 紀錄 使用者名稱 -> ConnectionId
        private static readonly Dictionary<string, string> UserConnections = new Dictionary<string, string>();

        public async Task RegisterUser(string userName)
        {
            // 檢查字典裡是否已經有這個人的連線，且連線還活著
            if (UserConnections.ContainsKey(userName))
            {
                // ✨ 先進為王：對「現在這個新連線 (Caller)」發送重複偵測訊號
                await Clients.Caller.SendAsync("OnDuplicateTabDetected");
            }
            else
            {
                // 如果是第一個分頁，才記錄連線 ID
                UserConnections[userName] = Context.ConnectionId;
            }
        }

        // 當連線中斷（關閉分頁）時，移除紀錄，這樣下次開啟才不會被擋
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var item = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (item.Key != null)
            {
                UserConnections.Remove(item.Key);
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async Task NotifyTakeoverComplete(string orderId, string newOwner)
        {
            // 廣播給該訂單群組：交接已完成
            await Clients.Group(orderId).SendAsync("OnDataSynced", newOwner);
        }
    }
}