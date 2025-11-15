using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;


namespace WpfApp1
{
    public class mainview : INotifyPropertyChanged
    {
        private readonly ResfullApi Api;
        private readonly WebSocketing Socket;
        public ObservableCollection<string> RoomNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Message { get; } = new ObservableCollection<string>();
        public IRelayCommand Joincommand { get; set; }
        public IRelayCommand Leavecommand { get; set; }
        public IRelayCommand SendMessageCommand { get; set; }
        public IRelayCommand ReloadRoomCommand { get; set; }
        private string _inputMessage;
        public string InputMessage
        {
            get => _inputMessage;
            set { _inputMessage = value; 
                OnPropertyChanged(); }
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value;
                OnPropertyChanged(); }
        }

        private string _userName = string.Empty; 
        public string UserName
        {
            get => _userName;
            set { _userName = value; 
                OnPropertyChanged(); }
        }
        private string _selectRoom = string.Empty;
        public string SelectRoom
        {
            get => _selectRoom;
            set { _selectRoom = value; 
                OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public mainview()
        {
            Api = new ResfullApi();
            Socket = new WebSocketing();
            Socket.Connected += () =>
            {
                Application.Current.Dispatcher?.Invoke(() =>
                {
                    Status = "웹소켓 연결됨";
                });
            };
            Socket.Disconnected += () =>
            {
                Application.Current.Dispatcher?.Invoke(() =>
                {
                    Status = "웹소켓 해제";
                });
            };
            Socket.Error += ex =>
            {
                Application.Current.Dispatcher?.Invoke(() =>
                {
                    Status = ex;
                });
            };
            Socket.Message += msg =>
            {
                Application.Current.Dispatcher?.Invoke(() =>
                {

                    TryHandle(msg);
                });
            };
            //_ 이건 액션 파라미터 인자 object를 받아야 하지만 파리미터가 없는 함수를 바인딩할때  인자는 있는데 쓰지 않을거라는 표시를 남겨주어야함
            ReloadRoomCommand = new IRelayCommand(async _ => await LoadRoomsAsync(),
                                           _ => Socket.GetConnected);

            Joincommand = new IRelayCommand(async r => await joinroom(r as string),
                                               r => r is string && Socket.GetConnected);

            Leavecommand= new IRelayCommand(async _ => await Leavenroom(),
                                           _ => Socket.GetConnected);


            SendMessageCommand = new IRelayCommand(async _ => await SendMessageaSync(),
                                                  _ => !string.IsNullOrWhiteSpace(InputMessage)
                                                       && SelectRoom != null
                                                       && Socket.GetConnected);

        }
        public async Task InitializeAsync()
        {
            try
            {
                SelectRoom = "선택된 방이 없음";
                UserName = "Wpf User1";
                Status = "웹소켓 연결 시도 중...";
                await Socket.ConnectedAsync("ws://localhost:8080/ws/chat"); // 🔧 서버 주소 맞게 수정

                if (Socket.GetConnected)
                {
                    Status = "웹소켓 연결됨. 방 목록 불러오는 중...";
                    await LoadRoomsAsync();

                    Status = "준비 완료";
                }
            }
            catch (Exception ex)
            {
                Status = "초기화 실패: " + ex.Message;
            }
        }

        //  서버에서 받아온 제이슨 파일을 정제해서 유저에게 뿌리는 역할
        private void TryHandle(string msg)
        {
            try
            { 
                var json = JsonDocument.Parse(msg).RootElement;
                string type = json.GetProperty("type").GetString();
                string roomid = json.GetProperty("type").GetString();
                string sender = json.GetProperty("sender").GetString();
                string content = json.GetProperty("content").GetString();

                if(string.IsNullOrEmpty(SelectRoom) || !string.IsNullOrEmpty(SelectRoom)&& roomid != SelectRoom)
                    return;

                switch (type)
                {
                    case "chat":
                        Message.Add($"{sender}:{content}");
                        break;
                    case "join":
                        Message.Add($"[시스템] {sender}님이 '{roomid}' 방에 입장했습니다.");
                        break;
                    case "leave":
                        Message.Add($"[시스템] {sender}님이 '{roomid}' 방에서 나갔습니다.");
                        break;
                }
            }
            catch(Exception e)
            {
                Message.Add("[서버 원본] " + msg);
            }
        }
        private async Task SendMessageaSync()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || SelectRoom == null)
                return;
            var text = InputMessage;
            InputMessage = string.Empty;
            var playload = new
            {
                type = "chat",
                roomid = SelectRoom,
                Sender = UserName,
                content = text
            };
            string json = JsonSerializer.Serialize(playload);
            await Socket.SendtheMessageAsync(json);
        }
        private async Task joinroom(string roomtitle)
        {
            if (string.IsNullOrWhiteSpace(roomtitle))
                return;
            SelectRoom = roomtitle;
            Message.Clear();
            var playload = new
            {
                type = "join",
                roomid = SelectRoom,
                Sender = UserName,
                content = ""
            };
            string json = JsonSerializer.Serialize(playload);
            await Socket.SendtheMessageAsync(json);
        }
        private async Task Leavenroom()
        {
            Message.Clear();
            var playload = new
            {
                type = "leave",
                roomid = SelectRoom,
                Sender = UserName,
                content = ""
            };
            SelectRoom = "선택된 방이 없음";
            string json = JsonSerializer.Serialize(playload);
            await Socket.SendtheMessageAsync(json);
        }
        private async Task LoadRoomsAsync()
        {
            RoomNames.Clear();
            var list = await Api.GetRoomsAsync(); // List<string>
            foreach (var roomTitle in list)
                RoomNames.Add(roomTitle);
        }
    }
}
