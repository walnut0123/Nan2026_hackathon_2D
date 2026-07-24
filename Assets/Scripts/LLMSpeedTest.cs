using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

public class LLMSpeedTest : MonoBehaviour
{
    // Gist에 저장된 "최신 리비전" 고정 주소 (커밋 해시 없는 버전 - 항상 최신 내용을 반환함)
    private const string GIST_RAW_URL = "https://gist.githubusercontent.com/walnut0123/a0e016f97ecd1ac3588749b710584140/raw/tunnel_url";

    private string serverUrl = ""; // 앱 시작 시 Gist에서 자동으로 채워짐
    private bool serverUrlReady = false;

    private string inputText = "카드게임 규칙을 한 문장으로 설명해줘";
    private string lastReply = "";
    private string statusText = "대기 중";
    private bool isWaiting = false;

    private GUIStyle labelStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;

    // 화면 크기 기준(1080x2340, S24)으로 비율 계산 후 실제 Screen.width/height에 맞춰 스케일링
    private void InitStyles()
    {
        float scaleFactor = Screen.width / 1080f; // 기준 해상도 대비 배율

        // [수정 규칙 4 반영] 모바일 화면에서 비정상적으로 크지 않도록 폰트 크기를 적절하게 하향 조정
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = Mathf.RoundToInt(24f * scaleFactor); // 기존 40f -> 24f로 최적화
        labelStyle.normal.textColor = Color.white;
        labelStyle.wordWrap = true;

        textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = Mathf.RoundToInt(24f * scaleFactor); // 기존 40f -> 24f로 최적화
        textFieldStyle.alignment = TextAnchor.MiddleLeft;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = Mathf.RoundToInt(26f * scaleFactor); // 기존 45f -> 26f로 최적화

        stylesInitialized = true;
    }

    void Start()
    {
        StartCoroutine(FetchServerUrl());
    }

    IEnumerator FetchServerUrl()
    {
        // 캐시 방지: raw.githubusercontent.com이 몇 분간 이전 내용을 캐싱해서 돌려주는 문제가 있어
        // 매 요청마다 다른 타임스탬프 쿼리를 붙여 항상 최신 Gist 내용을 받아오도록 함
        string cacheBustUrl = GIST_RAW_URL + "?t=" + System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using (UnityWebRequest webRequest = UnityWebRequest.Get(cacheBustUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string fetchedUrl = webRequest.downloadHandler.text.Trim();
                if (!fetchedUrl.EndsWith("/v1/chat/completions"))
                {
                    if (!fetchedUrl.EndsWith("/")) fetchedUrl += "/";
                    fetchedUrl += "v1/chat/completions";
                }
                serverUrl = fetchedUrl;
                serverUrlReady = true;
                statusText = "서버 URL 로드 완료 (대기 중)";
                Debug.Log($"<color=cyan>[속도 테스트]</color> URL 매핑 성공: {serverUrl}");
            }
            else
            {
                statusText = "서버 URL 로드 실패";
                Debug.LogError($"[속도 테스트] Gist에서 URL을 가져오지 못했습니다: {webRequest.error}");
            }
        }
    }

    void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitStyles();
        }

        float scaleFactor = Screen.width / 1080f;

        // [수정 규칙 3 반영] 씬 전환 UI 버튼 영역(X:40, Y:-40, 크기 300x100)과의 완전히 겹치는 현상 방지
        // 시작 Y좌표(startY)를 씬 버튼 크기와 마진을 고려하여 충분히 아래(180)로 이격했습니다.
        float startX = 40f * scaleFactor;
        float startY = 180f * scaleFactor; // 기존 40f -> 180f로 하향 조정하여 아래로 내림
        float uiWidth = Screen.width - (startX * 2);

        float elementHeight = 65f * scaleFactor; // 폰트 축소에 맞춰 엘리먼트 세로 폭도 자연스럽게 조정 (기존 90f -> 65f)
        float spacing = 15f * scaleFactor;

        // 1. 상태 표시 레이블
        GUI.Label(new Rect(startX, startY, uiWidth, elementHeight), $"상태: {statusText}", labelStyle);
        startY += elementHeight + spacing;

        // 2. 입력란 제목
        GUI.Label(new Rect(startX, startY, uiWidth, elementHeight), "요청할 프롬프트 입력:", labelStyle);
        startY += elementHeight + (spacing * 0.5f);

        // 3. 텍스트 필드 입력창
        inputText = GUI.TextField(new Rect(startX, startY, uiWidth, elementHeight), inputText, textFieldStyle);
        startY += elementHeight + spacing;

        // 4. 전송 버튼
        if (serverUrlReady && !isWaiting)
        {
            if (GUI.Button(new Rect(startX, startY, uiWidth, elementHeight * 1.2f), "LLM 요청 전송 (속도 측정)", buttonStyle))
            {
                StartCoroutine(SendLLMRequest(inputText));
            }
        }
        else
        {
            string disableReason = isWaiting ? "답변을 기다리는 중..." : "서버 주소 불러오는 중...";
            GUI.Box(new Rect(startX, startY, uiWidth, elementHeight * 1.2f), disableReason, buttonStyle);
        }
        startY += (elementHeight * 1.2f) + (spacing * 2f);

        // 5. AI 결과 출력 컴포넌트
        if (!string.IsNullOrEmpty(lastReply))
        {
            GUI.Label(new Rect(startX, startY, uiWidth, elementHeight), "최근 AI 답변:", labelStyle);
            startY += elementHeight + (spacing * 0.5f);

            float remainingHeight = Screen.height - startY - (40f * scaleFactor);
            GUI.Label(new Rect(startX, startY, uiWidth, remainingHeight), lastReply, labelStyle);
        }
    }

    IEnumerator SendLLMRequest(string message)
    {
        isWaiting = true;
        statusText = "요청 중...";

        var payload = new
        {
            model = "default",
            messages = new[]
            {
                new { role = "user", content = message }
            },
            temperature = 0.7
        };

        string jsonBody = JsonConvert.SerializeObject(payload);
        var request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Stopwatch stopwatch = Stopwatch.StartNew();

        yield return request.SendWebRequest();

        stopwatch.Stop();
        long elapsedMs = stopwatch.ElapsedMilliseconds;

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JObject.Parse(request.downloadHandler.text);
            string reply = response["choices"][0]["message"]["content"].ToString();

            lastReply = reply;
            statusText = $"성공 | 응답 시간: {elapsedMs} ms ({elapsedMs / 1000.0:F2}초)";
            Debug.Log($"<color=lime>[속도 테스트]</color> {elapsedMs} ms 소요 | 응답: {reply}");
        }
        else
        {
            statusText = $"실패 | 경과 시간: {elapsedMs} ms | 에러: {request.error}";
            Debug.LogError($"[속도 테스트] API 호출 실패: {request.error}");
        }

        isWaiting = false;
    }
}
