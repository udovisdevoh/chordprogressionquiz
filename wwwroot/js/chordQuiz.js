// wwwroot/js/chordQuiz.js

/**
 * Parses a Roman numeral string into its base degree and type, and calculates its relative semitones.
 * This is a simplified client-side version. For full accuracy, a more robust parser might be needed.
 * @param {string} roman - The Roman numeral string (e.g., "I", "ii", "V7", "bIII").
 * @returns {object|null} An object with { baseSemitones: int, qualitySuffix: string, pitchesRelative: int[], fullRoman: string, baseRomanNoSuffix: string, basePitchesRelative: int[] } or null if invalid.
 */
function parseRomanNumeralForQuiz(roman) {
    roman = roman.trim();
    if (!roman) return null;

    // Simplified chord quality intervals relative to its *own root*
    const qualityMap = {
        '': [0, 4, 7],         // Major Triad
        'm': [0, 3, 7],        // Minor Triad
        'dim': [0, 3, 6],      // Diminished Triad
        'aug': [0, 4, 8],      // Augmented Triad
        '7': [0, 4, 7, 10],    // Dominant 7th
        'm7': [0, 3, 7, 10],   // Minor 7th
        'maj7': [0, 4, 7, 11], // Major 7th
        'dim7': [0, 3, 6, 9],  // Diminished 7th
        'm7b5': [0, 3, 6, 10], // Half-diminished 7th
        'sus2': [0, 2, 7],
        'sus4': [0, 5, 7],
        'add9': [0, 4, 7, 14],
        'add#9': [0, 4, 7, 15],
        'maj7#11': [0, 4, 7, 11, 18],
        'm(maj7)': [0, 3, 7, 11]
    };

    // Intervals for *just the triad* based on inferred quality, for 'base match'
    const triadQualityMap = {
        '': [0, 4, 7],         // Major Triad
        'm': [0, 3, 7],        // Minor Triad
        'dim': [0, 3, 6],      // Diminished Triad
        'aug': [0, 4, 8]       // Augmented Triad
    };


    // Semitones from C for diatonic degrees in C Major (I=0, ii=2, iii=4, IV=5, V=7, vi=9, vii=11)
    const majorScaleDegrees = {
        'I': 0, 'II': 2, 'III': 4, 'IV': 5, 'V': 7, 'VI': 9, 'VII': 11,
        'i': 0, 'ii': 2, 'iii': 4, 'iv': 5, 'v': 7, 'vi': 9, 'vii': 11
    };

    // Regex to extract accidental, base roman, and quality suffix (case-insensitive for suffixes)
    const regex = /^(b|#)?([IVXivx]+)(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m|\(maj7\))?$/i;
    const match = roman.match(regex);

    if (!match) return null;

    const leadingAccidental = match[1];
    const baseRomanRaw = match[2]; // e.g., 'I', 'i'
    const qualitySuffixRaw = match[3] || ''; // e.g., 'Maj7', '7', 'm'

    // Determine the base Roman numeral string without any suffix for comparison
    const baseRomanNoSuffix = (leadingAccidental || '') + baseRomanRaw;


    let baseDiatonicSemitones = majorScaleDegrees[baseRomanRaw.toUpperCase()];
    if (baseDiatonicSemitones === undefined) return null;

    if (leadingAccidental === 'b') baseDiatonicSemitones--;
    else if (leadingAccidental === '#') baseDiatonicSemitones++;

    baseDiatonicSemitones = (baseDiatonicSemitones % 12 + 12) % 12;

    let finalQualitySuffix = ''; // Will be the key for qualityMap (e.g., 'maj7', 'm', '7')
    let inferredTriadQuality = ''; // Will be the key for triadQualityMap (e.g., '', 'm')

    // Determine the final quality suffix for map lookup and the inferred triad quality
    if (qualitySuffixRaw === '') {
        if (baseRomanRaw.match(/^[IVX]+$/)) { // Uppercase base Roman -> Major triad
            finalQualitySuffix = '';
            inferredTriadQuality = '';
        } else if (baseRomanRaw.match(/^[ivx]+$/)) { // Lowercase base Roman -> Minor triad
            finalQualitySuffix = 'm';
            inferredTriadQuality = 'm';
        }
    } else {
        const lowerQualitySuffix = qualitySuffixRaw.toLowerCase();
        if (lowerQualitySuffix === '7') { // Special handling for bare '7'
            if (baseRomanRaw.match(/^[IVX]+$/)) { // Uppercase base Roman + '7' -> Dominant 7th
                finalQualitySuffix = '7';
                inferredTriadQuality = ''; // Dominant 7th is built on Major triad
            } else { // Lowercase base Roman + '7' -> Minor 7th (e.g., i7)
                finalQualitySuffix = 'm7';
                inferredTriadQuality = 'm'; // Minor 7th is built on Minor triad
            }
        } else if (lowerQualitySuffix.startsWith('maj')) { // Covers 'maj' and 'maj7'
            finalQualitySuffix = lowerQualitySuffix;
            inferredTriadQuality = ''; // Major 7th is built on Major triad
        } else if (lowerQualitySuffix.startsWith('m')) { // Covers 'm', 'm7', 'm7b5', 'm(maj7)'
            finalQualitySuffix = lowerQualitySuffix;
            inferredTriadQuality = 'm'; // Minor based
        } else if (lowerQualitySuffix.includes('dim')) { // Covers 'dim' and 'dim7'
            finalQualitySuffix = lowerQualitySuffix;
            inferredTriadQuality = 'dim'; // Diminished based
        } else if (lowerQualitySuffix === 'aug') {
            finalQualitySuffix = lowerQualitySuffix;
            inferredTriadQuality = 'aug';
        } else {
            // For other explicit suffixes like 'sus2', 'sus4', 'add9', 'add#9', etc.
            // For base match, their implied triad is generally based on the uppercase/lowercase of the roman.
            finalQualitySuffix = lowerQualitySuffix;
            if (baseRomanRaw.match(/^[IVX]+$/)) { // If base is Major
                inferredTriadQuality = '';
            } else if (baseRomanRaw.match(/^[ivx]+$/)) { // If base is Minor
                inferredTriadQuality = 'm';
            }
        }
    }

    const intervals = qualityMap[finalQualitySuffix];
    if (!intervals) {
        return null;
    }

    const chordPitchesRelative = intervals.map(interval => (baseDiatonicSemitones + interval) % 12);
    chordPitchesRelative.sort((a, b) => a - b);

    // Calculate base triad pitches
    const baseTriadIntervals = triadQualityMap[inferredTriadQuality];
    const basePitchesRelative = baseTriadIntervals ? baseTriadIntervals.map(interval => (baseDiatonicSemitones + interval) % 12) : [];
    basePitchesRelative.sort((a, b) => a - b);


    return {
        baseSemitones: baseDiatonicSemitones,
        qualitySuffix: finalQualitySuffix, // Full quality (e.g., 'maj7', '7', 'm')
        pitchesRelative: chordPitchesRelative, // Pitches for the full chord
        fullRoman: roman, // The trimmed user input for strict text comparison

        baseRomanNoSuffix: baseRomanNoSuffix, // e.g., "V", "i", "bIII"
        inferredTriadQuality: inferredTriadQuality, // e.g., '', 'm', 'dim', 'aug'
        basePitchesRelative: basePitchesRelative // Pitches for just the triad
    };
}


/**
 * Compares two sets of pitches (semitones 0-11) for strict equality.
 * @param {Array<number>} pitches1 - Array of pitches (0-11 semitones relative to tonic).
 * @param {Array<number>} pitches2 - Array of pitches (0-11 semitones relative to tonic).
 * @returns {boolean} True if pitches are identical, false otherwise.
 */
function arePitchesExactlyEqual(pitches1, pitches2) {
    if (!pitches1 || !pitches2) return false;
    if (pitches1.length !== pitches2.length) return false;

    for (let i = 0; i < pitches1.length; i++) {
        if (pitches1[i] !== pitches2[i]) return false;
    }
    return true;
}

/**
 * Compares two sets of pitches (semitones 0-11) for similarity (at least N identical notes regardless of octave).
 * @param {Array<number>} pitches1 - Array of pitches (0-11 semitones relative to tonic).
 * @param {Array<number>} pitches2 - Array of pitches (0-11 semitones relative to tonic).
 * @param {number} minIdenticalNotes - Minimum number of identical notes required for "similar".
 * @returns {boolean} True if similar, false otherwise.
 */
function arePitchesSimilar(pitches1, pitches2, minIdenticalNotes = 2) {
    if (!pitches1 || !pitches2 || pitches1.length === 0 || pitches2.length === 0) {
        return false;
    }

    const set1 = new Set(pitches1.map(p => p % 12));
    const set2 = new Set(pitches2.map(p => p % 12));

    let commonNotes = 0;
    set1.forEach(pitch => {
        if (set2.has(pitch)) {
            commonNotes++;
        }
    });

    return commonNotes >= minIdenticalNotes;
}

// Map semitone offset from C to note name (sharps preferred for simplicity)
const semitonesToNoteName = [
    "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
];

// Map Roman numeral quality suffix to its common chord name suffix
const romanQualityToChordSuffix = {
    '': '',         // Major Triad (C -> C)
    'm': 'm',       // Minor Triad (i -> Am)
    'dim': 'dim',   // Diminished Triad (vii° -> Bdim)
    'aug': 'aug',   // Augmented Triad (III+ -> Eaug)
    '7': '7',       // Dominant 7th (V7 -> G7)
    'm7': 'm7',     // Minor 7th (ii7 -> Dm7)
    'maj7': 'maj7', // Major 7th (Imaj7 -> Cmaj7)
    'dim7': 'dim7', // Diminished 7th (vii°7 -> Bdim7)
    'm7b5': 'm7b5', // Half-diminished 7th (iiø7 -> Dm7b5)
    'sus2': 'sus2',
    'sus4': 'sus4',
    'add9': 'add9',
    'add#9': 'add#9',
    'maj7#11': 'maj7#11',
    'm(maj7)': 'm(maj7)'
};

/**
 * Converts a Roman numeral string from a given relative key to its absolute chord name.
 * This function mimics part of the server-side ChordProgressionService logic.
 * @param {string} romanNumeral - The Roman numeral string (e.g., "I", "V7", "bIII").
 * @param {string} relativeToKeyString - The key string (e.g., "C Major", "A Aeolian").
 * @param {number} globalTransposeOffset - The global MIDI transpose offset applied to the progression.
 * @returns {string} The absolute chord name (e.g., "C", "G7", "Ebmaj7").
 */
function romanToAbsoluteChordName(romanNumeral, relativeToKeyString, globalTransposeOffset) {
    const parsedRoman = parseRomanNumeralForQuiz(romanNumeral);
    if (!parsedRoman) {
        return romanNumeral; // Return original if parsing fails
    }

    // Parse the relativeToKeyString to get the tonic note name and scale type
    const keyParts = relativeToKeyString.split(' ');
    let keyRootNoteName = keyParts[0].trim();
    // keyScaleType is not directly used for note calculation here but could be for diatonic logic
    // let keyScaleType = keyParts.slice(1).join(' ').trim();


    // Map note name to semitones from C (0-11)
    const noteNameToSemitonesFromC = {
        "C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6,
        "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11
    };
    const keyRootSemitones = noteNameToSemitonesFromC[keyRootNoteName];
    if (keyRootSemitones === undefined) {
        return romanNumeral; // Cannot determine absolute root if key root is unknown
    }

    // Calculate the chord's root semitones relative to C (0-11)
    // parsedRoman.baseSemitones is the diatonic degree + accidental relative to the key's *tonic* (e.g., I=0, ii=2)
    // So, adding keyRootSemitones correctly positions it absolutely.
    let absoluteChordRootSemitones = (keyRootSemitones + parsedRoman.baseSemitones);

    // Apply the global transpose offset
    absoluteChordRootSemitones = (absoluteChordRootSemitones + globalTransposeOffset);

    // Normalize to 0-11 semitones within an octave
    absoluteChordRootSemitones = (absoluteChordRootSemitones % 12 + 12) % 12;

    // Convert absolute semitones to note name
    const chordRootNoteName = semitonesToNoteName[absoluteChordRootSemitones];

    // Get the common chord suffix
    const chordSuffix = romanQualityToChordSuffix[parsedRoman.qualitySuffix];
    if (chordSuffix === undefined) {
        return chordRootNoteName + parsedRoman.qualitySuffix; // Use original suffix if not mapped
    }

    return chordRootNoteName + chordSuffix;
}


/**
 * Checks the user's answers against the actual progression and provides visual feedback.
 * @param {object} quizData - Object containing actual answers (quiz answers, tonal, modal, midi pitches).
 */
function checkAnswers(quizData) {
    const inputElements = document.querySelectorAll('.chord-guess-input');
    const displayBoxes = document.querySelectorAll('.chord-display-box');

    const actualQuizAnswersRaw = quizData.actualQuizAnswers; // Array of actual Roman numeral strings
    const actualAbsoluteMidiPitches = quizData.absoluteMidiPitches;

    inputElements.forEach((input, index) => {
        // Skip the tonic chords as they are pre-revealed and shouldn't be re-evaluated
        // (index === 0 was the previous logic, now we need to identify by content)
        const currentChordRoman = actualQuizAnswersRaw[index];
        if (currentChordRoman.toLowerCase() === 'i') {
            return; // Skip pre-revealed tonic chords
        }


        const guessedRoman = input.value.trim();
        const displayBox = displayBoxes[index];

        displayBox.classList.remove('correct', 'incorrect', 'similar'); // Clear previous feedback

        const parsedGuessedChord = parseRomanNumeralForQuiz(guessedRoman);
        const parsedActualChord = parseRomanNumeralForQuiz(actualQuizAnswersRaw[index]); // Parse the actual answer as well

        if (parsedGuessedChord && parsedActualChord) {
            // 1. Strict Exact String Match (e.g., "V7" === "V7")
            if (parsedGuessedChord.fullRoman === parsedActualChord.fullRoman) {
                displayBox.classList.add('correct');
            }
            // 2. Full Structural Match (same root, same quality type, same full set of notes)
            else if (parsedGuessedChord.baseSemitones === parsedActualChord.baseSemitones &&
                parsedGuessedChord.qualitySuffix === parsedActualChord.qualitySuffix &&
                arePitchesExactlyEqual(parsedGuessedChord.pitchesRelative, parsedActualChord.pitchesRelative)) {
                displayBox.classList.add('correct');
            }
            // 3. Base Chord Match (same root, same inferred TRIAD quality, ignoring 7ths, add9, etc.)
            else if (parsedGuessedChord.baseSemitones === parsedActualChord.baseSemitones &&
                parsedGuessedChord.inferredTriadQuality === parsedActualChord.inferredTriadQuality &&
                arePitchesExactlyEqual(parsedGuessedChord.basePitchesRelative, parsedActualChord.basePitchesRelative)) {
                displayBox.classList.add('correct');
            }
            // 4. Similarity Match (at least 2 common notes in the full chord)
            else if (arePitchesSimilar(parsedGuessedChord.pitchesRelative, parsedActualChord.pitchesRelative, 2)) {
                displayBox.classList.add('similar');
            }
            // 5. If none of the above, it's incorrect
            else {
                displayBox.classList.add('incorrect');
            }
        } else {
            // If either input (guessed or actual) couldn't be parsed, mark as incorrect
            displayBox.classList.add('incorrect');
        }
    });
}

/**
 * Reveals all hidden answers (song name, roman numerals, etc.) and highlights correct/incorrect/similar.
 * @param {object} quizData - Object containing actual answers.
 */
function revealAnswers(quizData) {
    // Show hidden info containers
    document.getElementById('revealInfoSection').style.display = 'block';

    // Reveal main quiz info
    document.getElementById('quizInfoSongName').textContent = quizData.actualSongName;
    document.getElementById('quizInfoKeysExample').textContent = quizData.actualKeysExample;
    document.getElementById('quizInfoRelativeTo').textContent = quizData.actualRelativeTo;
    document.getElementById('quizInfoTonalRoman').textContent = quizData.actualTonalRomanNumerals.join(' ');

    // Display Quiz Answer Source
    document.getElementById('quizAnswerSource').textContent = quizData.quizAnswerSource;

    const modalRomanElement = document.getElementById('quizInfoModalRoman');
    modalRomanElement.innerHTML = '';
    if (quizData.actualModalRomanNumerals && quizData.actualModalRomanNumerals.length > 0) {
        quizData.actualModalRomanNumerals.forEach(modal => {
            const span = document.createElement('span');
            span.innerHTML = `${modal.RomanNumerals || ''} (relative to ${modal.RelativeTo || ''})<br />`;
            modalRomanElement.appendChild(span);
        });
        document.getElementById('quizInfoModalSection').style.display = 'block';
    } else {
        document.getElementById('quizInfoModalSection').style.display = 'none';
    }

    // Display Played Key and Actual Chords in Played Key
    const playedKeyElement = document.getElementById('quizInfoPlayedKey');
    const actualChordsPlayedKeyElement = document.getElementById('quizInfoActualChordsPlayedKey');

    if (playedKeyElement && actualChordsPlayedKeyElement) {
        const baseKeyString = quizData.actualRelativeTo;
        const transposeOffset = quizData.globalTransposeOffset;

        const keyParts = baseKeyString.split(' ');
        const keyRootNoteName = keyParts[0].trim();
        const keyScaleType = keyParts.slice(1).join(' ').trim() || 'Major';

        const keyRootSemitones = semitonesToNoteName.indexOf(keyRootNoteName);
        if (keyRootSemitones !== -1) {
            const transposedRootSemitones = (keyRootSemitones + transposeOffset + 12) % 12;
            const transposedRootName = semitonesToNoteName[transposedRootSemitones];
            const playedKeyName = `${transposedRootName} ${keyScaleType}`;
            playedKeyElement.textContent = playedKeyName;
        } else {
            playedKeyElement.textContent = "N/A (Key Parse Error)";
        }

        let relativeToKeyForChordConversion;
        if (quizData.quizAnswerSource === "Modal" && quizData.actualModalRomanNumerals && quizData.actualModalRomanNumerals.length > 0) {
            relativeToKeyForChordConversion = quizData.actualModalRomanNumerals[0].RelativeTo;
        } else {
            relativeToKeyForChordConversion = quizData.actualRelativeTo;
        }

        const actualChordsInPlayedKey = quizData.actualQuizAnswers.map(roman => {
            return romanToAbsoluteChordName(roman, relativeToKeyForChordConversion, quizData.globalTransposeOffset);
        });
        actualChordsPlayedKeyElement.textContent = actualChordsInPlayedKey.join(' ');
    }


    // Also run checkAnswers to ensure coloring happens and populate reveal boxes
    // Note: checkAnswers will now skip the tonic chords for coloring, which is fine as they're pre-colored.
    checkAnswers(quizData);

    const inputElements = document.querySelectorAll('.chord-guess-input');
    const chordRevealBoxes = document.querySelectorAll('.chord-reveal-box');
    const actualQuizAnswers = quizData.actualQuizAnswers;

    inputElements.forEach((input, index) => {
        const actualRomanForReveal = actualQuizAnswers[index];
        const revealBox = chordRevealBoxes[index];

        // Fill in the reveal box with the actual preferred chord name
        revealBox.textContent = actualRomanForReveal;
    });
}

// Expose functions globally
window.chordQuiz = {
    checkAnswers: checkAnswers,
    revealAnswers: revealAnswers
};

// NEW: Function to reveal all tonic (I or i) chords on page load
function revealTonicChords(quizData) {
    if (quizData.actualQuizAnswers && quizData.actualQuizAnswers.length > 0) {
        const inputElements = document.querySelectorAll('.chord-guess-input');
        const displayBoxes = document.querySelectorAll('.chord-display-box');
        const chordRevealBoxes = document.querySelectorAll('.chord-reveal-box');

        quizData.actualQuizAnswers.forEach((actualRoman, index) => {
            // Check if the current chord is 'I' or 'i' (case-insensitive)
            if (actualRoman.toLowerCase() === 'i') {
                const tonicInput = inputElements[index];
                const tonicDisplayBox = displayBoxes[index];
                const tonicRevealBox = chordRevealBoxes[index];

                if (tonicInput && tonicDisplayBox && tonicRevealBox) {
                    tonicInput.value = actualRoman; // Pre-fill input
                    tonicInput.disabled = true; // Disable input
                    tonicDisplayBox.textContent = actualRoman; // Show in display box
                    tonicDisplayBox.classList.add('correct'); // Mark as correct
                    tonicRevealBox.textContent = actualRoman; // Show in reveal box
                }
            }
        });
    }
}

// Add revealTonicChords to the DOMContentLoaded listener in ChordQuiz.cshtml
// It will be called after quizData is available.