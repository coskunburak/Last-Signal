using UnityEngine;
namespace LastSignal.Gathering
{
    public enum TreeAudioEvent { WoodHit, BarkFracture, StructuralCrack, Creak, JointFailure, FallingMotion, GroundImpact, BranchContact, Settling }
    [CreateAssetMenu(menuName="Last Signal/Gathering/Tree Audio Bank")]
    public sealed class TreeAudioBank : ScriptableObject
    {
        [Tooltip("Imported recordings for owner audition. These are not automatically mapped by filename.")]
        public AudioClip[] auditionCandidates;
        public AudioClip woodHit, barkFracture, structuralCrack, creak, fracture, fallingMotion, groundImpact, branchContact, settling;
        public AudioClip Clip(TreeAudioEvent kind) => kind switch
        {
            TreeAudioEvent.WoodHit=>woodHit, TreeAudioEvent.BarkFracture=>barkFracture,
            TreeAudioEvent.StructuralCrack=>structuralCrack, TreeAudioEvent.Creak=>creak,
            TreeAudioEvent.JointFailure=>fracture, TreeAudioEvent.FallingMotion=>fallingMotion,
            TreeAudioEvent.GroundImpact=>groundImpact, TreeAudioEvent.BranchContact=>branchContact,
            TreeAudioEvent.Settling=>settling, _=>null
        };
        [Tooltip("Set only after listening, trimming unsuitable tails and verifying source rights.")]
        public bool mappingApproved;
        [Tooltip("Audition mapped clips only in Editor FirstTownTreePhysics validation scenes. Does not approve production audio.")]
        public bool previewInValidationScene;
        public bool CanPlay(string scenePath) => mappingApproved || (Application.isEditor && previewInValidationScene &&
            !string.IsNullOrEmpty(scenePath) && scenePath.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_",System.StringComparison.Ordinal) &&
            scenePath.EndsWith(".unity",System.StringComparison.Ordinal));
    }
}
