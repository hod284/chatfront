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
    private Button RefreshButton;
    [SerializeField]
    private Button CrerateButton;
    [SerializeField]
    private Button LeaveButton;
    [SerializeField]
    private  Image Message;
    [SerializeField]
    private  GameObject Messageparent;
    [SerializeField]
    private Button roomButtonPrefab;
    [SerializeField]
    private GameObject roomButtonParent;
    public string serverUrl = "ws://localhost:8080/chat";
    public string roomsApiUrl = "http://localhost:8080/api/rooms";
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
        RefreshButton.onClick.AddListener(async() => {
           await LoadRoomsAsync();
        });
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
                LoadRoomsAsync();
                break;

            case "system":
                Debug.Log($"[SYSTEM][{msg.roomId}] {msg.content}");
                break;

            case "Chat":
                handleMessage($"{msg.sender}: {msg.content}");
                break;

            case "Leave":
                handleMessage($"{msg.roomId}]: {msg.content}");
                for(int i = 1; i < Messageparent.transform.childCount; i++)
        {
                    Destroy(Messageparent.transform.GetChild(i).gameObject);
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

    // ========== 공통 전송 함수 ==========

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

    // ========== 방 생성 ==========

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

    // ========== 방 참가 ==========

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

    // ========== 채팅 보내기 ==========

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

    // ========== 방 나가기 ==========

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
        LoadRoomsAsync();
        RoomName.text  =currentRoomId = "";
    }

    // ========== 종료 시 정리 ==========

    async void OnApplicationQuit()
    {
        if (ws != null)
        {
            await ws.Close();
        }
    }
    public async Task LoadRoomsAsync()
    {
        using (UnityWebRequest uwr = UnityWebRequest.Get(roomsApiUrl))
        {
            var op = uwr.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

     

            string rawJson = uwr.downloadHandler.text;
            Debug.Log("방 목록 RAW JSON: " + rawJson); // 예: ["room-1","room-2"]

            // JsonUtility는 배열을 바로 못 읽어서 한 번 감싸줌
            string wrapped = "{\"rooms\":" + rawJson + "}";
            var wrapper = JsonUtility.FromJson<StringArrayWrapper>(wrapped);

            CreateRoomButtons(wrapper.rooms);
        }
    }

    private void CreateRoomButtons(string[] rooms)
    {
        // 기존 버튼들 제거
        for (int i =1; i< roomButtonParent.transform.childCount; i++)
        {
            Destroy(roomButtonParent.transform.GetChild(i).gameObject);
        }

        // 새로운 버튼 생성
        foreach (var room in rooms)
        {
            var btn = Instantiate(roomButtonPrefab, roomButtonParent.transform);
            btn.GetComponentInChildren<TMP_Text>().text = room;
            btn.gameObject.SetActive(true);
            string capturedRoom = room; // 클로저용 복사

            btn.onClick.AddListener(() =>
            {
                Debug.Log("방 클릭: " + capturedRoom);
                // 여기서 웹소켓으로 참가
                JoinRoom(capturedRoom);
            });
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
