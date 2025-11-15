using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public class mainview : INotifyPropertyChanged
    {
        private readonly ResfullApi Api;
        private readonly WebSocketing Socket;
        public ObservableCollection<string> RoomNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Message { get; } = new ObservableCollection<string>();
        public IRelayCommand Joincommand;
        public IRelayCommand SendMessage;
        public IRelayCommand ReloadRoomCommand;
         private string _inputMessage;
        public string InputMessage
        {
            get => _inputMessage;
            set { _inputMessage = value; 
                OnPropertyChanged(); }
        }

        private string _status = "초기화 전";
        public string Status
        {
            get => _status;
            set { _status = value;
                OnPropertyChanged(); }
        }

        private string _userName = "User1";   // 🔧 여기 너 닉네임 / 로그인 정보로 바꾸면 됨
        public string UserName
        {
            get => _userName;
            set { _userName = value; 
                OnPropertyChanged(); }
        }
        private string _selectRoom = "";   
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
        
        }
        private void TryHandle(string msg)
        {

            try
            { 
                       
            
            }
            catch(Exception e)
            { }

        }

    }
}
