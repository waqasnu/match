using UnityEngine;
using System.Collections;

public class PingPongAnimator : MonoBehaviour {

    public string ping;
    public string pong;

    string to;

    Animation anim;

	// Use this for initialization
	void Awake () {
        anim = GetComponent<Animation>();
        to = "pong";
        anim.Play(pong);
        anim[pong].time = anim[pong].length;
        anim.enabled = true;
        anim.Sample();
        anim.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
        if (to == "pong" && anim[pong].time < anim[pong].length) {
            anim[pong].time += Time.unscaledDeltaTime;
            anim.enabled = true;
            anim.Sample();
            anim.enabled = false;
            return;
        }
        if (to == "ping" && anim[ping].time < anim[ping].length) {
            anim[ping].time += Time.unscaledDeltaTime;
            anim.enabled = true;
            anim.Sample();
            anim.enabled = false;
            return;
        }
        if (anim.isPlaying)
            anim.Stop();
	}

    public void Hit() {
        if (to == "pong" && anim[pong].time >= anim[pong].length) {
            to = "ping";
            anim.Play(ping);
            anim[ping].time = 0;

            return;
        }
        if (to == "ping" && anim[ping].time >= anim[ping].length) {
            to = "pong";
            anim.Play(pong);
            anim[pong].time = 0;
            return;
        }
    }
}
