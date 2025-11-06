using UnityEngine;
using UnityEngine.UI;


namespace Fixor {
    public static class ServiceLocator {
        public static PinReceptor ReceptorPrefab { get; private set; }
        public static Chip        ChipPrefab   { get; private set; }
        public static Pulser      PulserPrefab { get; private set; }
        public static Output      OutputPrefab { get; private set; }
        public static Button      ButtonPrefab { get; private set; }
        public static LevelDataSO LevelData    { get; private set; }
        
        
        public static void Initialiser(PinReceptor receptor, Chip chip, Pulser pulser, Output output, Button button, LevelDataSO levelData) {
            ReceptorPrefab = receptor;
            ChipPrefab   = chip;
            PulserPrefab = pulser;
            OutputPrefab = output;
            ButtonPrefab = button;
            LevelData  = levelData;
        }
    }
}