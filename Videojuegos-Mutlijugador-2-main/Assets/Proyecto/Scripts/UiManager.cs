using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class UiManager : MonoBehaviour
{
    public RectTransform PanelMainMenu;
    public TMP_Dropdown namesSelector;
    public RectTransform PanelClient;

    public RectTransform PanelHUD;
    public TMP_Text labelHealth;
    public GameObject playerNameTemplate;

    public List<string> namesList = new List<string>();
    public int selectedNameIndex { get { return namesSelector.value; } }

    public struct NamesData
    {
        public string[] names;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelMainMenu.gameObject.SetActive(true);
        PanelClient.gameObject.SetActive(false);
        PanelHUD.gameObject.SetActive(false);

        //namesSelector.ToString();
        GetNames();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator GetNamesFromServer()
    {
        string url = "http://monsterballgo.com/api/names";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            NamesData namesData = JsonUtility.FromJson<NamesData>(json);

            namesList.AddRange(namesData.names);
            namesSelector.AddOptions(namesList);
        }
    }

    public void GetNames()
    {
        namesSelector.ClearOptions();
        StartCoroutine(GetNamesFromServer());
    }


    public void ONButtonStartHost()
    {
        //crear partida
        NetworkManager.Singleton.StartHost();

        PanelMainMenu.gameObject.SetActive(false);
        PanelHUD.gameObject.SetActive(true);
    }

    public void OnButtonClientConnect()
    {
        GameObject go = GameObject.Find("inputIP");

        string ip = go.GetComponent<TMP_InputField>().text;
        Debug.Log("Se conceto a " +  ip);

        PanelMainMenu.gameObject.SetActive(false);
        PanelClient.gameObject.SetActive(false);
        PanelHUD.gameObject.SetActive(true);

        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ip;
        NetworkManager.Singleton.StartClient();
    } 

        
}
