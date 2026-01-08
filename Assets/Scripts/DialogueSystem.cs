//   TextAsset Dialogue System
//   By ThrowLab Games
//   November 2024

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ThrowLab.Systems.UI
{
    public class DialogueSystem : MonoBehaviour
    {
        /* =================[#]  CONFIGURATION  [#]================= */

        [Header("Character Print Settings")]
        [SerializeField] private PrintSpeeds defaultPrintSpeed = PrintSpeeds.Normal;
        [SerializeField, Range(0.001f, 1f)] private float verySlowPrintSpeed;
        [SerializeField, Range(0.001f, 1f)] private float slowPrintSpeed;
        [SerializeField, Range(0.001f, 1f)] private float normalPrintSpeed;
        [SerializeField, Range(0.001f, 1f)] private float fastPrintSpeed;
        [SerializeField, Range(0.001f, 1f)] private float veryFastPrintSpeed;
        [Space(5)]
        [SerializeField] private float fullStopPauseTime;
        [SerializeField] private float commaPauseTime;
        [SerializeField] private bool pauseAtFullStop;
        [SerializeField] private bool pauseAtComma;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip dialogueCharSfx;
        [SerializeField] private AudioClip dialogueProceedSfx;
        [SerializeField] private bool isFixedSfxTiming;
        [SerializeField] private float fixedSfxTiming;

        [Header("Dependencies")]
        [SerializeField] private Image dialogueCharacterImage;
        [SerializeField] private TextMeshProUGUI dialogueHeader;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image dialoguePromptImage;
        [SerializeField] private AudioSource dialogueSfxSource;

        /* =================[#]  INTERNAL VARIABLES/DEPENDENCIES  [#]================= */

        // Enums
        public enum PrintSpeeds { VerySlow, Slow, Normal, Fast, VeryFast };

        // Component dependencies
        private DialogueTransitions dialogueTransitions;
        private DialogueUtils dialogueUtils;

        // Internal values
        private List<string> dialogueLines;
        private int lineIteration = 0;
        private int lineCharIndex = 0;
        private PrintSpeeds currentPrintSpeedSetting = PrintSpeeds.Normal;

        // Internal flags
        private bool isEngaged = false;
        private bool isPrinting = false;
        private bool isHalted = false;
        private bool skipCheck;

        /* =================[#]  ACCESSORS/EVENT HOOKS  [#]================= */

        public static DialogueSystem Instance { get; private set; }
        /// <summary>
        /// The current dialogue print speed. Set the speed using 
        /// </summary>
        public float CurrentPrintSpeed
        {
            get
            {
                switch (currentPrintSpeedSetting)
                {
                    case PrintSpeeds.VerySlow:
                        return verySlowPrintSpeed;
                    case PrintSpeeds.Slow:
                        return slowPrintSpeed;
                    case PrintSpeeds.Normal:
                        return normalPrintSpeed;
                    case PrintSpeeds.Fast:
                        return fastPrintSpeed;
                    case PrintSpeeds.VeryFast:
                        return veryFastPrintSpeed;
                    default:
                        goto case PrintSpeeds.Normal;
                }
            }
        }

        /* =================[#]  LIFECYCLE FUNCTIONS  [#]================= */

        private void Awake()
        {
            // Create a singleton for the dialogue system as it only needs to have one instance
            if (Instance != null)
                Destroy(Instance);
            Instance = this;

            // Initialise the references to the other components
            dialogueUtils = GetComponent<DialogueUtils>();
            dialogueTransitions = GetComponent<DialogueTransitions>();
        }

        private void Update()
        {
            if (!isPrinting && isEngaged)
            {
                if (Input.GetKeyDown(KeyCode.E) && dialogueTransitions.ReadyToProceed)
                {
                    // TODO: Play the dialogue proceed sfx

                    // Run it again if it has not finished the list of dialogue lines yet
                    if (lineIteration < dialogueLines.Count) PrintDialogue();
                    // Exit the dialogue if it has finished on the last line
                    else if (lineIteration >= dialogueLines.Count)
                    {
                        isEngaged = false;
                        dialogueText.text = "";
                        dialogueTransitions.ExitDialogue();
                    }
                }
            }
        }

        /* =================[#]  PUBLIC DIALOGUE SYSTEM API  [#]================= */

        /// <summary>
        /// Saves the contents of the passed-through TextAsset to a list and begins the transition for the dialogue graphics.
        /// If you want to initialise the dialogue system, only call this function.
        /// </summary>
        /// <param name="dialogueBundle">The text file to be read from and printed to the dialogue system.</param>
        public void InitiateDialogue(TextAsset dialogueBundle)
        {
            isEngaged = true;
            isHalted = false;
            dialogueText.text = "";
            //dialogueHeader.text = "";
            dialoguePromptImage.enabled = false;

            if (dialogueLines != null) dialogueLines.Clear();
            dialogueLines = dialogueBundle.text
                .Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList(); // Split the text into lines, remove empty lines and then add it to the dialogueLines list.

            lineIteration = 0;
            CheckInlineArguments();
            dialogueTransitions.EnterDialogue();
        }

        /// <summary>
        /// Starts printing the dialogue to the dialogue box from a list of lines saved from the TextAsset passed through from the InitiateDialogue function.
        /// To initialise the dialogue system, call InitiateDialogue() instead.
        /// </summary>
        public void PrintDialogue()
        {
            StartCoroutine(StartDialoguePrinting());
        }

        private void CheckInlineArguments()
        {
            int minCharIndex = lineCharIndex;
            // Check inline argument the first time.
            if (dialogueLines[lineIteration][lineCharIndex] == '[')
            {
                string inlineTag = "";
                while (dialogueLines[lineIteration][lineCharIndex] != ']')
                {
                    inlineTag += dialogueLines[lineIteration][lineCharIndex];
                    lineCharIndex++;
                }

                inlineTag += ']';
                lineCharIndex++;
                dialogueUtils.ProcessInlineArgument(inlineTag);
                dialogueLines[lineIteration] =
                    dialogueLines[lineIteration].Remove(minCharIndex, lineCharIndex - minCharIndex);
                lineCharIndex = 0;
            }

            // If another inline argument comes right after, re-run the function.
            if (dialogueLines[lineIteration][lineCharIndex] == '[')
                CheckInlineArguments();
        }

        /* =================[#]  SEQUENCE COROUTINES  [#]================= */

        private IEnumerator StartDialoguePrinting()
        {
            lineCharIndex = 0;
            CheckInlineArguments();

            isPrinting = true;
            dialogueText.text = "";
            dialoguePromptImage.enabled = false;

            while (isHalted) yield return null;

            // Setting up the sound effect clip and timing for character printing.
            dialogueSfxSource.clip = dialogueCharSfx;
            if (isFixedSfxTiming)
                StartCoroutine(PlaySFXFixed());

            // Trim the edges of the dialogue line of any whitespace characters before starting.
            dialogueLines[lineIteration] = dialogueLines[lineIteration].TrimStart();
            dialogueLines[lineIteration] = dialogueLines[lineIteration].TrimEnd();

            // This is the actual dialogue printing code
            for (int i = 0; i < dialogueLines[lineIteration].Length; i++)
            {
                char c = dialogueLines[lineIteration][i];

                // Detect formatting tags and insert them immediately so they don't get printed
                if (c == '<')
                {
                    string argument = "";
                    int j = i; // Local iteration variable for iteration until a closing formatting bracket is detected
                    while (c != '>')
                    {
                        argument += c;
                        c = dialogueLines[lineIteration][++j];
                    }

                    dialogueText.text += argument;
                    i = j;
                    c = dialogueLines[lineIteration][i];
                }
                // Check for an inline argument in the middle of a line, e.g. for a text effect
                else if (c == '[')
                {
                    lineCharIndex = i;
                    CheckInlineArguments();
                    i += lineCharIndex;
                }

                dialogueText.text += c;

                // Play the sound
                if (!isFixedSfxTiming)
                {
                    // TODO: Pay the dialogue character type sound
                }

                // Pause the dialogue for sentence-ending punctuation.
                // Note the "is, or" instead of multiple "||"
                if (c is '.' or '?' or '!' 
                    && pauseAtFullStop 
                    && i != dialogueLines[lineIteration].Length - 1 
                    && dialogueLines[lineIteration][i] != '<')
                    yield return new WaitForSeconds(fullStopPauseTime);
                // Pause the dialogue system for a separate time for commas.
                else if (c == ',' 
                         && pauseAtComma 
                         && i != dialogueLines[lineIteration].Length - 1 
                         && dialogueLines[lineIteration][i] != '<')
                    yield return new WaitForSeconds(commaPauseTime);

                yield return new WaitForSeconds(CurrentPrintSpeed);
                if (skipCheck)
                {
                    // TODO: Set this up properly once inline argument parsing is implemented
                    dialogueText.text = dialogueLines[lineIteration]; 
                    break;
                }
            }

            dialoguePromptImage.enabled = true;
            isPrinting = false;
            skipCheck = false;
            lineIteration++;
        }

        private IEnumerator PlaySFXFixed()
        {
            while (isPrinting)
            {
                // TODO: Pay the dialogue character type sound
                yield return new WaitForSeconds(fixedSfxTiming);
            }
        }

        /* =================[#]  INPUT SYSTEM HOOKS  [#]================= */

        /// <summary>
        /// Handler for the Skip/Proceed input action for proceeding the dialogue.
        /// Intended for use by the PlayerInput component.
        /// </summary>
        public void OnProceed(InputAction.CallbackContext context)
        {
            if (context.performed && isPrinting)
                skipCheck = true;
        }
    }
}