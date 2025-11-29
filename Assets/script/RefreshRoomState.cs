using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class RefreshRoomState : MonoBehaviour
{
    [SerializeField]
    private Button roomButtonPrefab;
    [SerializeField]
    private GameObject roomButtonParent;
    public string roomsApiUrl = "http://localhost:8080/api/Rooms";
    [SerializeField]
    private Button RefreshButton;
    [SerializeField]
    private WebsocketManager WSManager;
    [Serializable]
    private class StringArrayWrapper
    {
        public string[] rooms;
    }
    private void Awake()
    {
        RefreshButton.onClick.AddListener(async () => {
            await LoadRoomsAsync();
        });
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
        for (int i = 1; i < roomButtonParent.transform.childCount; i++)
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
                WSManager.JoinRoom(capturedRoom);
            });
        }
    }
}
