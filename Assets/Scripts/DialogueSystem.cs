using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mitchel.DialogueSystem
{
    public class DialogueSystem : MonoBehaviour
    {
        /// <summary>
        /// TEXTASSET DIALOGUE SYSTEM FOR UNITY
        /// Please note that
        /// </summary>
        [Header("General Settings")]
        public float CharDelayTime;
        [SerializeField] private bool pauseAtFullStop;
        [SerializeField] private float fullStopPauseTime;
        [SerializeField] private bool pauseAtComma;
        [SerializeField] private float commaPauseTime;

        [Header("Audio Settings")]
        public AudioClip DialogueCharSfx;
        public AudioClip DialogueProceedSfx;
        [SerializeField] private bool isFixedSfxTiming;
        [SerializeField] private float fixedSfxTiming;

        [Header("Object References")]
        public Image dialogueCharacterImage;
        public TextMeshProUGUI dialogueHeader;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image dialoguePromptImage;
        [SerializeField] private AudioSource dialogueSfxSource;

        // =============== Internal value variables ===============
        [HideInInspector] public bool GoodToGo;
        [HideInInspector] public float OriginalCharDelayTime;
        [HideInInspector] public bool DialogueEngaged = false;
        
        private List<string> dialogueLines;
        private bool isPrinting;
        private bool skipCheck;
        private int lineIteration = 0;
        private int lineCharIndex = 0;
        
        private static DialogueSystem instance;

        // =========== Internal object reference variables ===========
        public DialogueTransitions dialogueTransitions;
        [SerializeField] private DialogueUtils dialogueUtils;

        private void Start()
        {
            dialogueUtils = GetComponent<DialogueUtils>();
            dialogueTransitions = GetComponent<DialogueTransitions>();

            OriginalCharDelayTime = CharDelayTime;
        }
        
        // Create a singleton for the dialogue system as it only needs to have one instance
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(instance);
            }

            instance = this;
        }
        public static DialogueSystem Instance => instance;

        /// <summary>
        /// Saves the contents of the passed-through TextAsset to a list and begins the transition for the dialogue graphics.
        /// If you want to initialise the dialogue system, only call this function.
        /// </summary>
        /// <param name="dialogueBundle">The text file to be read from and printed to the dialogue system.</param>
        public void InitiateDialogue(TextAsset dialogueBundle)
        {
            DialogueEngaged = true;
            GoodToGo = true;
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

        // Update is called once per frame
        void Update()
        {
            if (!isPrinting && DialogueEngaged)
            {
                if (Input.GetKeyDown(KeyCode.E) && dialogueTransitions.ReadyToProceed)
                {
                    // TODO: Play the dialogue proceed sfx

                    // Run it again if it has not finished the list of dialogue lines yet
                    if (lineIteration < dialogueLines.Count) PrintDialogue();
                    // Exit the dialogue if it has finished on the last line
                    else if (lineIteration >= dialogueLines.Count)
                    {
                        DialogueEngaged = false;
                        dialogueText.text = "";
                        dialogueTransitions.ExitDialogue();
                    }
                }
            }
            else if (isPrinting)
            {
                // Sets to true so that the system checks this and then skips the printing
                if (Input.GetKeyDown(KeyCode.E)) skipCheck = true;
            }
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
            {
                //Debug.Log("Re-running inline arguments check");
                CheckInlineArguments();
            }

            //Debug.Log("Inline arguments check complete");
        }

        private IEnumerator StartDialoguePrinting()
        {
            lineCharIndex = 0;
            CheckInlineArguments();

            isPrinting = true;
            dialogueText.text = "";
            dialoguePromptImage.enabled = false;

            while (!GoodToGo) yield return null;

            // Setting up the sound effect clip and timing for character printing.
            dialogueSfxSource.clip = DialogueCharSfx;
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
                if (c is '.' or '?' or '!' && pauseAtFullStop && (i != dialogueLines[lineIteration].Length - 1 &&
                                                                  dialogueLines[lineIteration][i] != '<'))
                    yield return new WaitForSeconds(fullStopPauseTime);
                // Pause the dialogue system for a separate time for commas.
                else if (c == ',' && pauseAtComma && (i != dialogueLines[lineIteration].Length - 1 &&
                                                      dialogueLines[lineIteration][i] != '<'))
                    yield return new WaitForSeconds(commaPauseTime);

                yield return new WaitForSeconds(CharDelayTime);
                if (skipCheck)
                {
                    dialogueText.text =
                        dialogueLines
                            [lineIteration]; // TODO: Set this up properly once inline argument parsing is implemented
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
    }
}