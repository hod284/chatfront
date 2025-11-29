using NativeWebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class WebsocketManager : MonoBehaviour
{
    [SerializeField]
    private Button SendButton;
    [SerializeField]
    private TMP_InputField MessageFiled;
    [SerializeField]
    private TMP_InputField RoomNameFiled;
    [SerializeField]
    private TMP_Text RoomName;
    [SerializeField] 
    private TMP_Text State;
    [SerializeField]
    private RefreshRoomState RoomState;
    [SerializeField]
    private Button CrerateButton;
    [SerializeField]
    private Button LeaveButton;
    [SerializeField]
    private  Image Message;
    [SerializeField]
    private  GameObject Messageparent;
    public string serverUrl = "ws://localhost:8080/chat";
    private WebSocket ws;
    private string userName = "UnityUser";   // 유니티 클라이언트 이름
    private string currentRoomId =string.Empty;
    [Serializable]
    public class ChatDto
    {
        public string type;    // "Create", "Join", "Chat", "Leave", "CreateAck", "system", "error" 등
        public string roomId;
        public string sender;
        public string content;
    }

    [Serializable]
    private class StringArrayWrapper
    {
        public string[] rooms;
    }
    // Update is called once per frame
    void Update()
    {
        ws?.DispatchMessageQueue();
    }
    async void Start()
    {
        await ConnectAsync();
    }

    public async Task ConnectAsync()
    {
        if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting))
        {
            State.text ="websocketalreadyconnected";
            return;
        }

        State.text = "trying: " + serverUrl;
        ws = new WebSocket(serverUrl);

        ws.OnOpen += () =>
        {
            State.text = "succed!";
        };

        ws.OnError += (e) =>
        {
            State.text = "fail: " + e;
        };

        ws.OnClose += (e) =>
        {
            State.text = "exit: " + e;
        };

        ws.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            Debug.Log(" RAW JSON: " + json);

            try
            {
                var msg = JsonUtility.FromJson<ChatDto>(json);
                HandleIncomingMessage(msg);
            }
            catch (Exception ex)
            {
                State.text = "sending JSON parse error: " + ex.Message;
            }
        };
        SendButton.onClick.AddListener(()=>SendChat(MessageFiled.text));

        CrerateButton.onClick.AddListener(() => CreateRoom(RoomNameFiled.text));
        LeaveButton.onClick.AddListener(() => LeaveRoom());
        try
        {
            await ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("connectedfail: " + ex.Message);
        }

    }
    private void HandleIncomingMessage(ChatDto msg)
    {
        // 서버 WebsocketHandler에서 보내는 type들:
        // "CreateAck", "system", "Chat", "Leave", "error" 등
      
        switch (msg.type)
        {
            case "Create":
                Debug.Log($"[서버] 방 생성 확인: {msg.roomId}");
                RoomState.LoadRoomsAsync().ContinueWith(t => { 
                Console.WriteLine("방 목록 갱신 완료");
                });
                break;

            case "system":
                Debug.Log($"[SYSTEM][{msg.roomId}] {msg.content}");
                break;

            case "Chat":
                handleMessage($"{msg.sender}: {msg.content}");
                break;

            case "Leave":
                handleMessage($"{msg.roomId}]: {msg.content}");
                if(msg.sender == userName)
                {
                    for (int i = 1; i < Messageparent.transform.childCount; i++)
                    {
                       Destroy(Messageparent.transform.GetChild(i).gameObject);
                    }
                }
                break;

            case "error":
                Debug.LogError($"[ERROR] {msg.content}");
                break;

            default:
                Debug.Log($"[UNKNOWN TYPE: {msg.type}] {msg.content}");
                break;
        }
    }


    private async Task SendAsync(ChatDto dto)
    {
        if (ws == null || ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("웹소켓이 열려 있지 않아서 메시지를 보낼 수 없음");
            return;
        }

        string json = JsonUtility.ToJson(dto);
        Debug.Log("보내는 JSON: " + json);
        await ws.SendText(json);
    }


    public async void CreateRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
            return;
        currentRoomId=RoomName.text = roomId;

        var dto = new ChatDto
        {
            type = "Create",       // ★ 서버 switch-case랑 대소문자 정확히 맞춤
            roomId = roomId,
            sender = userName,
            content = ""
        };

        await SendAsync(dto);
    }


    public async void JoinRoom(string roomId)
    {
        currentRoomId= RoomName.text = roomId;

        var dto = new ChatDto
        {
            type = "Join",         // ★ "Join"
            roomId = roomId,
            sender = userName,
            content = ""
        };

        await SendAsync(dto);
    }

    public async void SendChat(string text)
    {
        if (string.IsNullOrWhiteSpace(currentRoomId))
        {
            Debug.LogWarning("현재 참가 중인 방이 없음 (먼저 Create 또는 Join 호출 필요)");
            return;
        }
        var dto = new ChatDto
        {
            type = "Chat",         // ★ "Chat"
            roomId = currentRoomId,
            sender = userName,
            content = text
        };
        MessageFiled.text ="";
        await SendAsync(dto);
    }


    public async void LeaveRoom()
    {
        if (string.IsNullOrWhiteSpace(currentRoomId))
        {
            Debug.LogWarning("나갈 방이 없음 (currentRoomId 비어 있음)");
            return;
        }

        var dto = new ChatDto
        {
            type = "Leave",        // ★ "Leave"
            roomId = currentRoomId,
            sender = userName,
            content = ""
        };

        await SendAsync(dto);
        await RoomState.LoadRoomsAsync();
        RoomName.text =currentRoomId = "";
    }
    async void OnApplicationQuit()
    {
        if (ws != null)
        {
            await ws.Close();
        }
    }
   
    private void handleMessage(string me)
    {
        var pre = Instantiate(Message, Messageparent.transform);
        var te = pre.GetComponentInChildren<Text>();
        pre.gameObject.SetActive(true);
        te.text = me;
    }
}
