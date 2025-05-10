using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mitchel.DialogueSystem
{   
    public class DialogueUtils : MonoBehaviour
{
    [Header("Dialogue Box Colours")]
    [SerializeField] private Color quinnColour;
    [SerializeField] private Color quinnHeaderColour;
    [Space(5)]
    [SerializeField] private Color caspianColour;
    [SerializeField] private Color caspianHeaderColour;
    [Space(5)]
    [SerializeField] private Color kinggColour;
    [SerializeField] private Color kinggHeaderColour;    
    [Space(5)]
    [SerializeField] private Color dewdropColour;
    [SerializeField] private Color dewdropHeaderColour;

    [Header("Inline Arguments Settings")] 
    [SerializeField] private float slowTextSpeedMultiplier;
    [SerializeField] private float fastTextSpeedMultiplier;
    [SerializeField] private float veryFastTextSpeedMultiplier;
    
    [Header("Character Sprites")]
    [Tooltip("Insert in the following order: Neutral, Happy, Surprised.")] [SerializeField] private Sprite[] quinnSprites;
    [Tooltip("Insert in the following order: Neutral, Happy, Surprised.")] [SerializeField] private Sprite[] caspianSprites;
    [Tooltip("Insert in the following order: Neutral, Happy, Surprised, Scared.")] [SerializeField] private Sprite[] kinggSprites;
    [Tooltip("Insert in the following order: Neutral, Happy, Surprised, Scared, Unhappy, Confused.")] [SerializeField] private Sprite[] dewdropSprites;

    // =============== Private value variables ===============
    [HideInInspector] public enum SelectedCharacter { Quinn, Caspian, Kingg, Dewdrop }
    public SelectedCharacter SelectedCharacterEnum { get; private set; } = SelectedCharacter.Dewdrop;
    private Dictionary<(SelectedCharacter, string), Sprite> expressionSprites;
    private List<string> savedTag = new List<string>();
    private string selectedEmotion = "Neutral";
    private bool characterSwitchReady = false;

    // =========== Private object reference variables ===========
    private DialogueSystem dialogueSys;
    private DialogueTransitions dialogueTransitions;

    // Declaring the dictionary of different expressions for each character
    private void Start()
    {
        dialogueSys = GetComponent<DialogueSystem>();
        dialogueTransitions = GetComponent<DialogueTransitions>();

        // TODO: Fix issue where each character MUST have all six entries, otherwise the system cannot find the sprite if the last character used an emotion that is out of range of the current character's dictionary.
        expressionSprites = new Dictionary<(SelectedCharacter, string), Sprite>
        {
            // Quinn emotions dictionary
            {(SelectedCharacter.Quinn, "Neutral"), quinnSprites[0]},
            {(SelectedCharacter.Quinn, "Happy"), quinnSprites[1]},
            {(SelectedCharacter.Quinn, "Surprised"), quinnSprites[2]},
            {(SelectedCharacter.Quinn, "Scared"), quinnSprites[2]}, // UNUSED
            {(SelectedCharacter.Quinn, "Unhappy"), quinnSprites[2]}, // UNUSED
            {(SelectedCharacter.Quinn, "Confused"), quinnSprites[2]}, // UNUSED
            // Caspian emotions dictionary
            {(SelectedCharacter.Caspian, "Neutral"), caspianSprites[0]},
            {(SelectedCharacter.Caspian, "Happy"), caspianSprites[1]},
            {(SelectedCharacter.Caspian, "Surprised"), caspianSprites[2]},
            {(SelectedCharacter.Caspian, "Scared"), caspianSprites[2]}, // UNUSED
            {(SelectedCharacter.Caspian, "Unhappy"), caspianSprites[2]}, // UNUSED
            {(SelectedCharacter.Caspian, "Confused"), caspianSprites[2]}, // UNUSED
            // Kingg emotions dictionary
            {(SelectedCharacter.Kingg, "Neutral"), kinggSprites[0]},
            {(SelectedCharacter.Kingg, "Happy"), kinggSprites[1]},
            {(SelectedCharacter.Kingg, "Surprised"), kinggSprites[2]},
            {(SelectedCharacter.Kingg, "Scared"), kinggSprites[3]},
            {(SelectedCharacter.Kingg, "Unhappy"), kinggSprites[3]}, // UNUSED
            {(SelectedCharacter.Kingg, "Confused"), kinggSprites[3]}, // UNUSED
            // Dewdrop emotions dictionary
            {(SelectedCharacter.Dewdrop, "Neutral"), dewdropSprites[0]},
            {(SelectedCharacter.Dewdrop, "Happy"), dewdropSprites[1]},
            {(SelectedCharacter.Dewdrop, "Surprised"), dewdropSprites[2]},
            {(SelectedCharacter.Dewdrop, "Scared"), dewdropSprites[3]},
            {(SelectedCharacter.Dewdrop, "Unhappy"), dewdropSprites[4]},
            {(SelectedCharacter.Dewdrop, "Confused"), dewdropSprites[5]}
        };
    }

    private void OnEnable()
    {
        DialogueTransitions.SpriteFadeInFinish += ReadyCharacterSwitch;
        DialogueTransitions.SpriteFadeOutFinish += UnreadyCharacterSwitch;
    }

    private void OnDisable()
    {
        DialogueTransitions.SpriteFadeInFinish -= ReadyCharacterSwitch;
        DialogueTransitions.SpriteFadeOutFinish -= UnreadyCharacterSwitch;
    }

    /// <summary>
    /// Process in-line arguments presented in square brackets to give special instructions
    /// to the dialogue system (e.g. to change character, character expression or text effect).
    /// </summary>
    /// <param name="tag">The in-line argument captured by the dialogue system, enclosed in square brackets.</param>
    public void ProcessInlineArgument(string tag)
    {
        if (savedTag != null) savedTag.Clear();

        savedTag = tag.Split(new[] { '[', '=', ']' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        for (int i = 0; i < savedTag.Count; i+=2)
        {
            switch (savedTag[i])
            {
                case "character":
                    ChangeCharacter(savedTag[++i]);
                    break;
                case "sprite":
                    ChangeExpression(savedTag[++i]);
                    break;
                case "texteffect":
                    ChangeTextEffect(savedTag[++i]);
                    break;
                case "paneleffect":
                    ChangePanelEffect(savedTag[++i]);
                    break;
                case "textspeed":
                    ChangeTextSpeed(savedTag[++i]);
                    break;
                case "position":
                    ChangeCharacterPosition(savedTag[++i]);
                    break;
                default:
                    Debug.LogError($"Unknown argument \"{tag}\". Did you misspell your argument?");
                    break;
            }
        }
    }

    private void ChangeCharacter(string input)
    {
        switch (input)
        {
            // TODO: After project timeline, find a way to shorten ChangeCharacter function
            case "Quinn":
                SelectedCharacterEnum = SelectedCharacter.Quinn;
                dialogueSys.dialogueHeader.text = "Quinn";

                if (expressionSprites.TryGetValue((SelectedCharacterEnum, selectedEmotion), out Sprite quinnSprite))
                {
                    dialogueTransitions.QueuedSprite = quinnSprite;
                    dialogueTransitions.CurrentColour = quinnColour;
                    dialogueTransitions.CurrentHeaderColour = quinnHeaderColour;
                    if (characterSwitchReady) 
                        dialogueTransitions.ChangeCharacter();
                }
                Debug.Log("Chosen character is Quinn");
                break;

            case "Caspian":
                SelectedCharacterEnum = SelectedCharacter.Caspian;
                dialogueSys.dialogueHeader.text = "Caspian";

                if (expressionSprites.TryGetValue((SelectedCharacterEnum, selectedEmotion), out Sprite caspianSprite))
                {
                    dialogueTransitions.QueuedSprite = caspianSprite;
                    dialogueTransitions.CurrentColour = caspianColour;
                    dialogueTransitions.CurrentHeaderColour = caspianHeaderColour;
                    if (characterSwitchReady)
                    {
                        dialogueTransitions.ChangeCharacter();
                    }
                }
                else Debug.Log("Caspian sprite retrieval failed.");
                Debug.Log("Chosen character is Caspian");
                break;

            case "Kingg":
                SelectedCharacterEnum = SelectedCharacter.Kingg;
                dialogueSys.dialogueHeader.text = "Kingg of Sticks";

                if (expressionSprites.TryGetValue((SelectedCharacterEnum, selectedEmotion), out Sprite kinggSprite))
                {
                    dialogueTransitions.QueuedSprite = kinggSprite;
                    dialogueTransitions.CurrentColour = kinggColour;
                    dialogueTransitions.CurrentHeaderColour = kinggHeaderColour;
                    if (characterSwitchReady)
                        dialogueTransitions.ChangeCharacter();
                }
                Debug.Log("Chosen character is Kingg");
                break;

            case "Dewdrop":
                SelectedCharacterEnum = SelectedCharacter.Dewdrop;
                dialogueSys.dialogueHeader.text = "Dewdrop";

                if (expressionSprites.TryGetValue((SelectedCharacterEnum, selectedEmotion), out Sprite dewdropSprite))
                {
                    dialogueTransitions.QueuedSprite = dewdropSprite;
                    dialogueTransitions.CurrentColour = dewdropColour;
                    dialogueTransitions.CurrentHeaderColour = dewdropHeaderColour;
                    if (characterSwitchReady)
                        dialogueTransitions.ChangeCharacter();
                }
                Debug.Log("Chosen character is Dewdrop");
                break;

            default:
                Debug.LogError($"No character data found for \"{input}\". Selected character will remain as \"{SelectedCharacterEnum}\".");
                break;
        }
    }

    private void ChangeExpression(string input)
    {
        // This will try and get whatever sprite is applicable depending on the character and expression detected
        if (expressionSprites.TryGetValue((SelectedCharacterEnum, input), out Sprite sprite))
        {
            if (!dialogueTransitions.ReadyToProceed)
            {
                dialogueTransitions.QueuedSprite = sprite;
            }
            else dialogueSys.dialogueCharacterImage.sprite = sprite;

            selectedEmotion = input;
            Debug.Log($"Character {SelectedCharacterEnum} is {input}");
        }
        else
        {
            Debug.LogWarning($"No sprite found for {SelectedCharacterEnum} with expression \"{input}\"");
        }
    }

    // TODO: Implement text effects for dialogue system
    private void ChangeTextEffect(string input)
    {
        Debug.Log(input);
    }

    // TODO: Implement panel effects for dialogue system
    private void ChangePanelEffect(string input)
    {
        Debug.Log(input);
    }

    private void ChangeTextSpeed(string input)
    {
        switch (input)
        {
            case "Slow":
                dialogueSys.CharDelayTime /= slowTextSpeedMultiplier;
                break;
            case "Normal":
                dialogueSys.CharDelayTime = dialogueSys.OriginalCharDelayTime;
                break;
            case "Fast":
                dialogueSys.CharDelayTime /= fastTextSpeedMultiplier;
                break;
            case "VeryFast":
                dialogueSys.CharDelayTime /= veryFastTextSpeedMultiplier;
                break;
            default:
                Debug.LogError($"INLINE ARGUMENT ERROR: Text speed {input} does not exist.");
                break;
        }
    }

    private void ChangeCharacterPosition(string input)
    {
        switch (input)
        {
            case "Left":
                dialogueTransitions.MainCharacterSide = true;
                Debug.Log("Left side chosen");
                break;
            case "Right":
                dialogueTransitions.MainCharacterSide = false;
                Debug.Log("Right side chosen");
                break;
        }
    }

    private void ReadyCharacterSwitch()
    {
        characterSwitchReady = true;
    }

    private void UnreadyCharacterSwitch()
    {
        characterSwitchReady = false;
    }
}
}