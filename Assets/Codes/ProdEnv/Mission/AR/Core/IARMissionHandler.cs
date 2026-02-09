using System;

namespace BaHanda.AR
{
    /// <summary>
    /// Contract for all AR mission handlers.
    /// Each mission type (GoBag, Evacuation, FirstAid, etc.) implements this interface.
    /// </summary>
    public interface IARMissionHandler
    {
        /// <summary>
        /// Whether the AR session is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Progress from 0 to 1 (0% to 100% complete)
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// Whether all objectives have been completed
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Start the AR experience for this mission
        /// </summary>
        void StartAR();

        /// <summary>
        /// End the AR experience (cleanup)
        /// </summary>
        void EndAR();

        /// <summary>
        /// Reset the AR mission to initial state
        /// </summary>
        void ResetAR();

        /// <summary>
        /// Pause AR interactions (but keep session alive)
        /// </summary>
        void PauseAR();

        /// <summary>
        /// Resume AR interactions
        /// </summary>
        void ResumeAR();

        // Events
        event Action OnARStarted;
        event Action OnAREnded;
        event Action OnARCompleted;
        event Action<float> OnProgressChanged;
    }
}
