using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Berry.Utils;

// The class is responsible for logic SimpleChip
[RequireComponent (typeof (Chip))]
public class SimpleChip : MonoBehaviour, IAnimateChip, IChipLogic {

	Chip _chip;
    public Chip chip {
        get {
            if (_chip == null)
                _chip = GetComponent<Chip>();
            return _chip;
        }
	}

    public Chip GetChip() {
        return chip;
    }

    public List<Chip> GetDangeredChips(List<Chip> stack) {
        stack.Add(chip);
        return stack;
    }

    public string GetChipType() {
        return "SimpleChip";
    }

    public int GetPotencial() {
        return 1;
    }

    public bool IsMatchable() {
        return true;
    }

	public IEnumerator Destroying (){
		chip.busy = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
		AudioAssistant.Shot("ChipCrush");
		
		yield return new WaitForSeconds(0.1f);
		
		chip.ParentRemove();
        chip.busy = false;

        chip.Play("Destroying");

        while (chip.IsPlaying("Destroying"))
            yield return 0;

        Destroy(gameObject);
	}

    public string[] GetClipNames() {
        return new string[] { "Destroying" };
    }

}