// wwwroot/js/chordQuiz.js

/**
 * Parses a Roman numeral string into its base degree and type, and calculates its relative semitones.
 * This is a simplified client-side version. For full accuracy, a more robust parser might be needed.
 * @param {string} roman - The Roman numeral string (e.g., "I", "ii", "V7", "bIII").
 * @returns {object|null} An object with { baseSemitones: int, qualitySuffix: string, pitchesRelative: int[], fullRoman: string } or null if invalid.
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

    // Semitones from C for diatonic degrees in C Major (I=0, ii=2, iii=4, IV=5, V=7, vi=9, vii=11)
    const majorScaleDegrees = {
        'I': 0, 'II': 2, 'III': 4, 'IV': 5, 'V': 7, 'VI': 9, 'VII': 11,
        'i': 0, 'ii': 2, 'iii': 4, 'iv': 5, 'v': 7, 'vi': 9, 'vii': 11
    };

    // Regex to extract accidental, base roman, and quality
    // This regex now is case-insensitive for the quality suffix part to catch variants like "Maj7" or "maj7"
    // but the base Roman numeral part ([IVXivx]+) is still case-sensitive for initial capture.
    const regex = /^(b|#)?([IVXivx]+)(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m|\(maj7\))?$/i; // Added 'i' flag for suffixes again
    const match = roman.match(regex);

    if (!match) return null;

    const leadingAccidental = match[1];
    const baseRomanRaw = match[2]; // e.g., 'I', 'i'
    let qualitySuffixRaw = match[3] || ''; // e.g., 'Maj7', '7', 'm'

    let baseDiatonicSemitones = majorScaleDegrees[baseRomanRaw.toUpperCase()];
    if (baseDiatonicSemitones === undefined) return null;

    if (leadingAccidental === 'b') baseDiatonicSemitones--;
    else if (leadingAccidental === '#') baseDiatonicSemitones++;

    baseDiatonicSemitones = (baseDiatonicSemitones % 12 + 12) % 12;

    let finalQualitySuffix = ''; // Will be the key for qualityMap (e.g., 'maj7', 'm', '7')

    // Determine the final quality suffix for map lookup
    if (qualitySuffixRaw === '') {
        if (baseRomanRaw.match(/^[IVX]+$/)) { // Uppercase base Roman -> Major triad
            finalQualitySuffix = '';
        } else if (baseRomanRaw.match(/^[ivx]+$/)) { // Lowercase base Roman -> Minor triad
            finalQualitySuffix = 'm';
        }
    } else {
        // Explicit suffixes, standardize to lowercase for map lookup
        const lowerQualitySuffix = qualitySuffixRaw.toLowerCase();
        if (lowerQualitySuffix === '7') { // Special handling for bare '7'
            if (baseRomanRaw.match(/^[IVX]+$/)) { // Uppercase base Roman + '7' -> Dominant 7th
                finalQualitySuffix = '7';
            } else { // Lowercase base Roman + '7' -> Minor 7th (e.g., i7)
                finalQualitySuffix = 'm7';
            }
        } else {
            // For other explicit suffixes like 'maj7', 'dim7', 'm7b5', 'm(maj7)', etc.
            // These should directly map to their lowercase form in qualityMap
            finalQualitySuffix = lowerQualitySuffix;
        }
    }

    const intervals = qualityMap[finalQualitySuffix];
    if (!intervals) {
        // console.warn(`Unknown or unhandled quality suffix: '${finalQualitySuffix}' for Roman: '${roman}'`);
        return null;
    }

    const chordPitchesRelative = intervals.map(interval => (baseDiatonicSemitones + interval) % 12);
    chordPitchesRelative.sort((a, b) => a - b); // Ensure pitches are sorted for consistent comparison

    return {
        baseSemitones: baseDiatonicSemitones,
        qualitySuffix: finalQualitySuffix,
        pitchesRelative: chordPitchesRelative,
        fullRoman: roman // Store the trimmed user input for strict text comparison
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

    // Assumes pitches are already sorted from parseRomanNumeralForQuiz
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
        const guessedRoman = input.value.trim();
        const displayBox = displayBoxes[index];

        displayBox.classList.remove('correct', 'incorrect', 'similar'); // Clear previous feedback

        const parsedGuessedChord = parseRomanNumeralForQuiz(guessedRoman);
        const parsedActualChord = parseRomanNumeralForQuiz(actualQuizAnswersRaw[index]); // Parse the actual answer as well

        // console.log(`--- Chord ${index} ---`);
        // console.log(`Guessed: '${guessedRoman}' Parsed:`, parsedGuessedChord);
        // console.log(`Actual: '${actualQuizAnswersRaw[index]}' Parsed:`, parsedActualChord);


        if (parsedGuessedChord && parsedActualChord) {
            // First, check for exact string match (case-sensitive)
            if (parsedGuessedChord.fullRoman === parsedActualChord.fullRoman) {
                displayBox.classList.add('correct');
            }
            // Second, check for structural match (same root, same quality type, same notes)
            // This handles cases like "I" vs "I" if both resolve to the same major triad structure,
            // or if there are subtle differences in parsing but structure is identical.
            // Also catches "i" vs "i" correctly after `parseRomanNumeralForQuiz` logic.
            else if (parsedGuessedChord.baseSemitones === parsedActualChord.baseSemitones &&
                     parsedGuessedChord.qualitySuffix === parsedActualChord.qualitySuffix &&
                     arePitchesExactlyEqual(parsedGuessedChord.pitchesRelative, parsedActualChord.pitchesRelative)) {
                displayBox.classList.add('correct');
            }
            // Third, check for similarity (at least 2 common notes)
            else if (arePitchesSimilar(parsedGuessedChord.pitchesRelative, parsedActualChord.pitchesRelative, 2)) {
                displayBox.classList.add('similar');
            }
            // If none of the above, it's incorrect
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

    // Also run checkAnswers to ensure coloring happens and populate reveal boxes
    const inputElements = document.querySelectorAll('.chord-guess-input');
    const chordRevealBoxes = document.querySelectorAll('.chord-reveal-box');

    const actualQuizAnswers = quizData.actualQuizAnswers; // Use the preferred answer for individual reveal boxes

    inputElements.forEach((input, index) => {
        const actualRomanForReveal = actualQuizAnswers[index];
        const revealBox = chordRevealBoxes[index];

        checkAnswers(quizData); // Re-run check to ensure colors are applied

        // Fill in the reveal box with the actual preferred chord name
        revealBox.textContent = actualRomanForReveal;
    });
}

// Expose functions globally
window.chordQuiz = {
    checkAnswers: checkAnswers,
    revealAnswers: revealAnswers
};