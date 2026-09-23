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
        [SerializeField] Text result;
        bool loading;
        public void Configure(SaveSession service,Button save,Button load,Text feedback)
        {saves=service;flow=service.GetComponent<SessionFlow>();saveButton=save;loadButton=load;result=feedback;}
        void OnEnable(){if(saveButton)saveButton.onClick.AddListener(Save);if(loadButton)loadButton.onClick.AddListener(Load);}
        void OnDisable(){if(saveButton)saveButton.onClick.RemoveListener(Save);if(loadButton)loadButton.onClick.RemoveListener(Load);StopAllCoroutines();loading=false;}
        void Update()
        {
            if(!flow)return;bool visible=flow.InMenu||(flow.Paused&&!flow.PreparationOpen);
            saveButton.gameObject.SetActive(visible&&!flow.InMenu);loadButton.gameObject.SetActive(visible&&flow.InMenu);
            saveButton.interactable=!loading&&!flow.PlayerDead&&!flow.Restoring;loadButton.interactable=!loading;
            result.gameObject.SetActive(visible);
        }
        void Save(){var outcome=saves.Save();result.text=outcome.Success?"Checkpoint saved.":outcome.Message;}
        void Load(){if(!loading&&flow.InMenu)StartCoroutine(LoadCheckpoint());}
        IEnumerator LoadCheckpoint(){loading=true;try{yield return saves.Load();result.text=saves.LastResult.Success?"Checkpoint loaded.":saves.LastResult.Message;}finally{loading=false;}}
    }
}
