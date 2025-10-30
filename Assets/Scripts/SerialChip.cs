using UnityEditor.Overlays;
using UnityEngine;

/*
namespace Fixor {
    [System.Serializable]
    public readonly struct SerialChip {
        public readonly string Name;
        public readonly Vector2 Position;

        public readonly SerialInputPin[] InputPins;
        public readonly string[] OutputNames; // something like Name_A, Name_B, Name_C

        public SerialChip(in SaveData data) {
            Name      = data.name;
            Position  = data.position;
            InputPins = data.InputPins;

            OutputNames = new string[data.numOutPins];
            for (int i = 0; i < OutputNames.Length; i++) {
                OutputNames[i] = $"{Name}_{(char)(65 + i)}";
                // Forme Name_A, Name_B, Name_C...
                // Eventually breaks but the uints for pin representation is likely to break first.
            }
        }
    }

    public readonly struct SerialInputPin {
        // pins belong to nodes, 

        
        
        public SerialInputPin(in SaveData data, PinReceptor pin) {
            
        }
    }
}





*/