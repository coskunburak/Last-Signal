using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.Audio
{
    [DisallowMultipleComponent] public sealed class AudioSettingsView : MonoBehaviour
    {
        public ProductionAudio audioOwner;
        public SessionFlow flow;
        public GameObject settingsPanel;
        public Slider[] sliders;
        public Toggle captions;
        public Text captionText;
        bool dirty;
        float saveAt;
        void Start()
        {
            for(int i=0;i<sliders.Length;i++)
            {
                int bus=i; sliders[i].SetValueWithoutNotify(audioOwner.Preferences[(AudioBus)i]);
                sliders[i].onValueChanged.AddListener(v=>SetLevel(bus,v));
            }
            captions.SetIsOnWithoutNotify(audioOwner.Preferences.Captions);
            captions.onValueChanged.AddListener(SetCaptions);
        }
        void SetLevel(int bus,float value)
        { audioOwner.Preferences[(AudioBus)bus]=value; audioOwner.ApplyPreferences(); Changed(); }
        void SetCaptions(bool value) { audioOwner.Preferences.Captions=value; Changed(); }
        void Changed() { dirty=true; saveAt=Time.unscaledTime+.5f; }
        void Flush() { if(dirty && audioOwner) { audioOwner.Preferences.Save(); dirty=false; } }
        void OnDisable() => Flush();
        void OnApplicationPause(bool pause) { if(pause) Flush(); }
        void OnApplicationQuit() => Flush();
        void Update()
        {
            settingsPanel.SetActive(flow && (flow.InMenu || flow.Paused) && !flow.PreparationOpen && !flow.JournalOpen);
            captionText.text=audioOwner.Preferences.Captions && audioOwner.Active?audioOwner.Caption:"";
            if(dirty && Time.unscaledTime>=saveAt) Flush();
        }
    }
}
