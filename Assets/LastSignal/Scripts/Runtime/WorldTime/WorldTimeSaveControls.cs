using System.Collections;
using LastSignal.Persistence;
using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.WorldTime
{
    /// <summary>Uses the existing save transaction; load is only offered at the session menu.</summary>
    public sealed class WorldTimeSaveControls : MonoBehaviour
    {
        [SerializeField] SaveSession saves;
        [SerializeField] SessionFlow flow;
        [SerializeField] Button saveButton, loadButton;
        public GameObject SaveButtonRoot => saveButton ? saveButton.gameObject : null;
        public GameObject LoadButtonRoot => loadButton ? loadButton.gameObject : null;
        [SerializeField] Text result;
        bool loading;
        float feedbackUntil;
        string checkpointPath;
        public void Configure(SaveSession service,Button save,Button load,Text feedback,string path = null)
        {saves=service;flow=service.GetComponent<SessionFlow>();saveButton=save;loadButton=load;result=feedback;checkpointPath=path;}
        void OnEnable(){if(saveButton)saveButton.onClick.AddListener(Save);if(loadButton)loadButton.onClick.AddListener(Load);}
        void OnDisable(){if(saveButton)saveButton.onClick.RemoveListener(Save);if(loadButton)loadButton.onClick.RemoveListener(Load);StopAllCoroutines();loading=false;feedbackUntil=0;if(result)result.text=string.Empty;}
        void Update()
        {
            if(!flow)return;bool visible=flow.Screen==SessionScreen.MainMenu||flow.Screen==SessionScreen.Pause;
            saveButton.gameObject.SetActive(visible&&!flow.InMenu);loadButton.gameObject.SetActive(visible&&flow.InMenu);
            saveButton.interactable=!loading&&!flow.PlayerDead&&!flow.Restoring;loadButton.interactable=!loading;
            bool feedbackVisible = Time.unscaledTime < feedbackUntil &&
                (visible || flow.Screen == SessionScreen.Gameplay);
            result.gameObject.SetActive(feedbackVisible || loading);
        }
        void ShowResult(SaveResult outcome, bool load)
        {
            result.text = SaveFeedback.Describe(outcome, load);
            feedbackUntil = Time.unscaledTime + 8f;
            result.gameObject.SetActive(true);
        }
        void Save(){ShowResult(saves.Save(checkpointPath), false);}
        void Load(){if(!loading&&flow.InMenu)StartCoroutine(LoadCheckpoint());}
        IEnumerator LoadCheckpoint()
        {
            loading=true;
            result.text="Kontrol noktası yükleniyor…";
            result.gameObject.SetActive(true);
            try{yield return saves.Load(checkpointPath);ShowResult(saves.LastResult, true);}
            finally{loading=false;}
        }
    }
}
