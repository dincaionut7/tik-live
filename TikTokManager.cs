using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using TikTokLiveSharp.Client;
using TikTokLiveSharp.Events;
using TikTokLiveUnity;

using System.Net.Http;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using TMPro;
using System;

using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class TikTokManager : MonoBehaviour
{
    public TextMeshProUGUI word_text;
    public TextMeshProUGUI word_definition;
    public TextMeshProUGUI number_text;
    public TextMeshProUGUI winner_text;

    public GameObject userDisplay;
    public GameObject userPanel;

    private string current_word;
    private List<Meaning> current_meanings;

    private TikTokLiveClient client;

    private TikTokLiveUnity.TikTokLiveManager tt_manager => TikTokLiveManager.Instance;

    private IWebDriver web_driver;

    private HttpClient http;


    // PRIVATES
    private void HookEvents()
    {
        tt_manager.OnConnected += OnConnection;
        tt_manager.OnJoin += OnUserJoined;
        tt_manager.OnChatMessage += OnComment;


    }
    private void Connect(string streamId)
    {
        bool connected = tt_manager.Connected;
        bool connecting = tt_manager.Connecting;
        if (connected || connecting)
            tt_manager.DisconnectFromLivestreamAsync();

        tt_manager.ConnectToStreamAsync(streamId, Debug.LogException);

        //client = new TikTokLiveClient(streamId, streamId);

        // Debug.Log($" connecting?{tt_manager.Connected}");

        // Debug.Log(client.Connected);
    }
    private void DisplayNextDefinition()
    {
        foreach (Meaning meaning in current_meanings)
        {
            if (word_definition.text.Contains(meaning.definition))
                continue;


            word_definition.text = $"{meaning.partOfSpeech}: {meaning.definition}";
            current_meanings.Remove(meaning);
            break;
        }
    }
    private void DisplayWord()
    {
        word_text.text = new string('*', current_word.Length) + $"\n\n{current_word.Length} Letters";
    }
    private async void SetupWord_Lib()
    {
        current_meanings = new List<Meaning>();

        HttpResponseMessage response = await http.GetAsync($"https://random-word-api.herokuapp.com/word");
        response.EnsureSuccessStatusCode();

        current_word = Regex.Replace(await response.Content.ReadAsStringAsync(), @"[\[\]""]", "").Trim();
        //Debug.Log(current_word);

        HttpRequestMessage definition_request = new HttpRequestMessage(HttpMethod.Get, $"https://wordsapiv1.p.rapidapi.com/words/{current_word}/definitions");
        definition_request.Headers.Add("x-rapidapi-host", "wordsapiv1.p.rapidapi.com");
        definition_request.Headers.Add("x-rapidapi-key", "cecef57917msh7c0b1bcc9a85556p1b2017jsnf1a22195e8b1");
        response = await http.SendAsync(definition_request);

        string definitions = await response.Content.ReadAsStringAsync();
        ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(definitions);

        if (!response.IsSuccessStatusCode || apiResponse.definitions == null || apiResponse.definitions.Count < 2)
        {
            SetupWord_Lib();
            return;
        }

        //Debug.Log($"Word: {apiResponse.word}");
        foreach (var meaning in apiResponse.definitions)
        {
            current_meanings.Add(meaning);
            //Debug.Log(meaning.definition);
        }


        DisplayWord();
        DisplayNextDefinition();
        StopAllCoroutines();
        StartCoroutine(StartTimer());

    }

    private float timeLeft;
    public TextMeshProUGUI time_left_text;


    private bool started = false;
    private IEnumerator StartTimer()
    {
        timeLeft = 120f;
        started = true;
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(1f); // Wait for 1 second       
            timeLeft -= 1f; // Decrease the time left by 1 second
            Debug.Log("Time left: " + timeLeft); // Optional: log the time left
            time_left_text.text = timeLeft.ToString();
        }

        Debug.Log("Timer finished!"); // Optional: log when the timer is done
        SetupWord_Lib();
        started = false;
    }

    private IEnumerator WordFound(Chat data)
    {
        StopAllCoroutines();
        word_text.text = $"{current_word} FOUND by {data.Sender.NickName}";
        updateWinner(data.Sender.NickName);

        //GameObject winner_displa = Instantiate(userDisplay, );

        yield return new WaitForSeconds(5);
        SetupWord_Lib();
    }
    

    // UNITY HOOKS
    void Awake()
    {

        
        http = new HttpClient();
        HookEvents();
        //SConnect("tt.rost4zxx\Connect(
        Connect("focus.fuel");

    }
    private void Start()
    {
        //WebDriverInit();
        SetupWord_Lib();
        
    }
    private void Update()
    {

    }

    private void updateWinner(string winnerName)
    {
        winner_text.text = $"Last Winner: {winnerName}";
    }
   

    // STATICS
    static async Task WaitForSecondsAsync(int seconds)
    {
        await Task.Delay(seconds * 1000);
    }
    

    // HOOKS
    private void OnUserJoined(object sender, Join data)
    {
        //Debug.Log("user joined: " + data.User.NickName);
    }
    private void OnConnection(object sender, bool e)
    {
        if (e != true)
        {
            //Debug.LogError("TT Connection Failed");
        }

        //if (client.Connected)
            //Debug.Log("Connection info: " + client.RoomInfo);
    }
    private void OnComment(object sender, Chat data)
    {
       // Debug.Log($"{data.Sender.NickName}: {data.Message}");
        if (string.Compare(data.Message, current_word, StringComparison.OrdinalIgnoreCase) == 0)
            StartCoroutine(WordFound(data));

        if (data.Message.Contains("cycle"))
        {
            Debug.Log("cycling");
            DisplayNextDefinition();
        }

        if (data.Message.Contains("new"))
        {
            SetupWord_Lib();
            //f
            //Debug.Log(" rofuwaa:" + current_word);
        }
    }
}

public class Meaning
{
    public string definition { get; set; }
    public string partOfSpeech { get; set; }
}

public class ApiResponse
{
    public string word { get; set; }
    public List<Meaning> definitions { get; set; }
}
