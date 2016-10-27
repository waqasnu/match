using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectParameters : MonoBehaviour {
    public static ProjectParameters main;

    void Awake() {
        main = this;
    }

    public bool square_combination = true;
    public float chip_acceleration = 20f;
    public float chip_max_velocity = 17f;
    public float swap_duration = 0.2f;
    public float slot_offset = 0.7f;
    public float music_volume_max = 0.4f;
}
