using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
internal static class CharacterSelectionVerification
{
    static int stage, frames;
    static double started, changed;
    static Component carousel;
    static RectTransform content, viewport;
    static string expected, before;
    static uint expectedHash;
    static bool success, hadBefore;
    static readonly List<string> logs = new List<string>();
    const string Screenshot = "Assets/CharacterSelectionVerification.png";
    static CharacterSelectionVerification()
    {
        string state = SessionState.GetString("CharacterVerification.State", "Idle");
        if (state == "WaitingForCompile") EditorApplication.delayCall += () => {
            SessionState.SetString("CharacterVerification.State", "EnteringPlayMode");
            EditorApplication.isPlaying = true;
        };
        if (state == "EnteringPlayMode" || state == "InPlayMode") {
            EditorApplication.playModeStateChanged += OnState;
            if (EditorApplication.isPlaying) Begin();
        }
    }
    static void OnState(PlayModeStateChange state) { if(state == PlayModeStateChange.EnteredPlayMode) Begin(); }
    static void Begin() {
        EditorApplication.playModeStateChanged -= OnState;
        SessionState.SetString("CharacterVerification.State", "InPlayMode");
        started = EditorApplication.timeSinceStartup;
        hadBefore = PlayerPrefs.HasKey("SelectedCharacterId");
        before = PlayerPrefs.GetString("SelectedCharacterId", "");
        Application.logMessageReceived += Capture;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }
    static void Capture(string text,string stack,LogType type) { if(logs.Count<60 && (text.Contains("[Test]") || type==LogType.Error || type==LogType.Exception)) logs.Add(text); }
    static Component Find(string name) {
        foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if(m != null && m.GetType().Name==name) return m;
        return null;
    }
    static object Field(object obj,string name) { return obj.GetType().GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj); }
    static void Call(object obj,string name,params object[] args) { obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args); }
    static void Tick() {
        if (++frames < 20) return;
        try {
            if(EditorApplication.timeSinceStartup-started>15) { Finish("Timeout stage="+stage); return; }
            if(stage==0) {
                var menu=Find("MainMenuManager"); Call(menu,"OpenMyItems");
                carousel=Find("SwipeCharacterCarousel");
                var chain=new List<Transform>(); for(var t=carousel.transform;t!=null;t=t.parent) chain.Add(t);
                for(int i=chain.Count-1;i>=0;i--) chain[i].gameObject.SetActive(true);
                var sr=carousel.GetComponent<UnityEngine.UI.ScrollRect>(); content=sr.content;viewport=sr.viewport;
                var definitions=(System.Collections.IList)Field(carousel,"cardDefinitions");
                object def=definitions[2]; expected=(string)Field(def,"characterId");
                var prefab=(GameObject)Field(def,"playerPrefab");
                expectedHash=(uint)Field(prefab.GetComponent<Unity.Netcode.NetworkObject>(),"GlobalObjectIdHash");
                Call(carousel,"ScrollRight");
                Debug.Log("[Test] Arrow selected="+PlayerPrefs.GetString("SelectedCharacterId"));
                Call(carousel,"OnCardClicked",2);
                Debug.Log("[Test] Click selected="+PlayerPrefs.GetString("SelectedCharacterId")+", expected="+expected);
                if(PlayerPrefs.GetString("SelectedCharacterId")!=expected) throw new Exception("Selection not saved");
                stage=1; changed=EditorApplication.timeSinceStartup; return;
            }
            if(stage==1 && EditorApplication.timeSinceStartup-changed>1) {
                var definitions=(System.Collections.IList)Field(carousel,"cardDefinitions");
                for(int index=0;index<definitions.Count;index++) {
                    object def=definitions[index];
                    expected=(string)Field(def,"characterId");
                    var prefab=(GameObject)Field(def,"playerPrefab");
                    expectedHash=(uint)Field(prefab.GetComponent<Unity.Netcode.NetworkObject>(),"GlobalObjectIdHash");
                    Call(carousel,"SnapImmediate",index);
                    var drag=new PointerEventData(EventSystem.current);
                    ExecuteEvents.Execute(carousel.gameObject,drag,ExecuteEvents.endDragHandler);
                    Debug.Log("[Test] Drag end expected="+expected+", saved="+PlayerPrefs.GetString("SelectedCharacterId"));
                    if(PlayerPrefs.GetString("SelectedCharacterId")!=expected) throw new Exception("Drag selection failed for "+expected);
                    var target=(RectTransform)content.GetChild(index);
                    var click=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,target.GetChild(1).position),button=PointerEventData.InputButton.Left};
                    var results=new List<RaycastResult>(); EventSystem.current.RaycastAll(click,results);
                    if(results.Count==0)throw new Exception("No UI raycast for "+expected);
                    var receiver=ExecuteEvents.ExecuteHierarchy(results[0].gameObject,click,ExecuteEvents.pointerClickHandler);
                    Debug.Log("[Test] UI click="+(receiver==null?"NULL":receiver.name)+", saved="+PlayerPrefs.GetString("SelectedCharacterId"));
                    if(receiver!=target.gameObject || PlayerPrefs.GetString("SelectedCharacterId")!=expected)throw new Exception("UI click failed for "+expected);
                }
                var card=(RectTransform)content.GetChild(3);
                var point=RectTransformUtility.WorldToScreenPoint(null,card.position);
                var delta=Vector3.Distance(card.position,viewport.position);
                Debug.Log("[Test] Card center="+point+", screen="+Screen.width+"x"+Screen.height+", alignment delta="+delta);
                var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,card.GetChild(1).position)};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                Debug.Log("[Test] Raycast="+string.Join(",",hits.ConvertAll(h=>h.gameObject.name)));
                ScreenCapture.CaptureScreenshot(Screenshot); SessionState.SetString("PlayModeTest.ScreenshotPath",Screenshot);
                stage=2; changed=EditorApplication.timeSinceStartup; return;
            }
            if(stage==2 && EditorApplication.timeSinceStartup-changed>0.2) {
                Call(Find("MainMenuManager"),"BackToMainMenu");
                Call(Find("MainMenuManager"),"StartSingleplayerGame");
                stage=3;changed=EditorApplication.timeSinceStartup;return;
            }
            if(stage==3 && EditorApplication.timeSinceStartup-changed>1) {
                var nm=Unity.Netcode.NetworkManager.Singleton;
                var player=nm!=null && nm.LocalClient!=null?nm.LocalClient.PlayerObject:null;
                Debug.Log("[Test] Listening="+(nm!=null&&nm.IsListening)+", player="+(player==null?"NULL":player.name)+", configured="+(nm==null||nm.NetworkConfig.PlayerPrefab==null?"NULL":nm.NetworkConfig.PlayerPrefab.name));
                if(player==null) {Finish("No local player");return;}
                uint actualHash=(uint)Field(player,"GlobalObjectIdHash");
                Debug.Log("[Test] Spawned hash="+actualHash+", expected hash="+expectedHash+", camera="+(Camera.main==null?"NULL":Camera.main.name));
                success=actualHash==expectedHash && PlayerPrefs.GetString("SelectedCharacterId")==expected;
                Finish(success?null:"Wrong player prefab");
            }
        } catch(Exception e) {Finish(e.ToString());}
    }
    [Serializable] class Result { public bool success; public string error; public string[] logs; }
    static void Finish(string error) {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        if(hadBefore)PlayerPrefs.SetString("SelectedCharacterId",before);else PlayerPrefs.DeleteKey("SelectedCharacterId");PlayerPrefs.Save();
        SessionState.SetString("CharacterVerification.Result",JsonUtility.ToJson(new Result{success=success,error=error,logs=logs.ToArray()}));
        SessionState.SetString("CharacterVerification.State","Done");EditorApplication.isPlaying=false;
    }
}
