// wwwroot/js/chordQuiz.js

/**
 * Parses a Roman numeral string into its base degree and type, and calculates its relative semitones.
 * This is a simplified client-side version. For full accuracy, a more robust parser might be needed.
 * @param {string} roman - The Roman numeral string (e.g., "I", "ii", "V7", "bIII").
 * @returns {object|null} An object with { baseSemitones: int, qualityIntervals: int[] } or null if invalid.
 */
function parseRomanNumeralForQuiz(roman) {
    // This is a simplified parser for quiz purposes. It tries to map common Roman numerals.
    // For full fidelity with ChordProgressionService, the logic is more complex (scales, accidentals).
    // Here, we'll try to get the "root" of the chord relative to a C tonic and its quality.

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
    // Example: bIII7, #IVdim, Vmmaj7
    const regex = /^(b|#)?(I|II|III|IV|V|VI|VII|i|ii|iii|iv|v|vi|vii)(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m|\(maj7\))?$/i;
    const match = roman.match(regex);

    if (!match) return null; // Invalid Roman numeral format

    const leadingAccidental = match[1];
    const baseRoman = match[2];
    let qualitySuffix = match[3] || '';

    let baseDiatonicSemitones = majorScaleDegrees[baseRoman.toUpperCase()];
    if (baseDiatonicSemitones === undefined) return null; // Should not happen if regex is good

    // Apply leading accidental to the diatonic degree
    if (leadingAccidental === 'b') baseDiatonicSemitones--;
    else if (leadingAccidental === '#') baseDiatonicSemitones++;

    // Normalize to 0-11 for comparisons
    baseDiatonicSemitones = (baseDiatonicSemitones % 12 + 12) % 12;

    // Infer quality based on casing if no explicit suffix
    if (qualitySuffix === '') {
        if (baseRoman.match(/^[IVX]+$/)) { // Uppercase (I, II, III...)
            qualitySuffix = ''; // Major
        } else if (baseRoman.match(/^[ivx]+$/)) { // Lowercase (i, ii, iii...)
            qualitySuffix = 'm'; // Minor
        }
    } else if (qualitySuffix.toLowerCase() === 'maj7') {
        qualitySuffix = 'maj7'; // Ensure correct key for map
    } else if (qualitySuffix.toLowerCase() === 'm7b5') {
        qualitySuffix = 'm7b5';
    } else if (qualitySuffix.toLowerCase() === 'm(maj7)') {
        qualitySuffix = 'm(maj7)';
    } else {
        qualitySuffix = qualitySuffix.toLowerCase(); // Standardize other suffixes
    }


    const intervals = qualityMap[qualitySuffix];
    if (!intervals) return null; // Unknown quality

    // Calculate absolute semitones (relative to tonic C=0)
    // The intervals are *relative to the chord's root*. So add baseDiatonicSemitones to each.
    const chordPitchesRelative = intervals.map(interval => (baseDiatonicSemitones + interval) % 12);

    return {
        baseSemitones: baseDiatonicSemitones, // Root of the chord (0-11)
        qualityIntervals: intervals, // Intervals relative to chord's root
        pitchesRelative: chordPitchesRelative // All pitches in the chord relative to the KEY'S tonic (0-11)
    };
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

    const set1 = new Set(pitches1.map(p => p % 12)); // Normalize to 0-11 and use a Set for quick lookup
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
 * @param {object} quizData - Object containing actual answers (roman numerals, midi pitches).
 */
function checkAnswers(quizData) {
    const inputElements = document.querySelectorAll('.chord-guess-input');
    const displayBoxes = document.querySelectorAll('.chord-display-box');
    const chordRevealBoxes = document.querySelectorAll('.chord-reveal-box');

    const actualTonalRomanNumerals = quizData.actualRomanNumerals; // Already an array of strings
    const actualAbsoluteMidiPitches = quizData.absoluteMidiPitches; // Array of arrays of int MIDI pitches

    inputElements.forEach((input, index) => {
        const guessedRoman = input.value.trim();
        const actualTonalRoman = actualTonalRomanNumerals[index]; // The correct Roman numeral for comparison
        const actualChordMidiPitches = actualAbsoluteMidiPitches[index]; // e.g., [60, 64, 67]

        const displayBox = displayBoxes[index];
        const revealBox = chordRevealBoxes[index];

        displayBox.classList.remove('correct', 'incorrect', 'similar'); // Clear previous feedback

        // Normalize actual MIDI pitches to 0-11 semitones relative to C (for comparison)
        // Assuming the first chord's root (midiPitch.min()) is the key's tonic for relative comparison.
        // Or, more simply, normalize all pitches to 0-11 and compare these.
        // For quiz, a direct 0-11 semitone comparison of notes in the chord is best.
        const actualPitchesNormalized = actualChordMidiPitches.map(p => p % 12);

        if (guessedRoman.toLowerCase() === actualTonalRoman.toLowerCase()) {
            displayBox.classList.add('correct');
        } else {
            const guessedChordParsed = parseRomanNumeralForQuiz(guessedRoman);
            if (guessedChordParsed) {
                // Compare pitches of the guessed chord (relative to C=0, 0-11 semitones)
                // to the actual chord pitches (relative to C=0, 0-11 semitones)
                if (arePitchesSimilar(guessedChordParsed.pitchesRelative, actualPitchesNormalized, 2)) {
                    displayBox.classList.add('similar');
                } else {
                    displayBox.classList.add('incorrect');
                }
            } else {
                displayBox.classList.add('incorrect'); // Invalid format or unrecognized
            }
        }
    });
}

/**
 * Reveals all hidden answers (song name, roman numerals, etc.) and highlights correct/incorrect/similar.
 * @param {object} quizData - Object containing actual answers.
 */
function revealAnswers(quizData) {
    // Reveal main quiz info
    document.getElementById('quizInfoSongName').textContent = quizData.actualSongName;
    document.getElementById('quizInfoKeysExample').textContent = quizData.actualKeysExample;
    document.getElementById('quizInfoRelativeTo').textContent = quizData.actualRelativeTo;
    document.getElementById('quizInfoTonalRoman').textContent = quizData.actualRomanNumerals.join(' '); // Join array to string

    const modalRomanElement = document.getElementById('quizInfoModalRoman');
    modalRomanElement.innerHTML = ''; // Clear previous
    if (quizData.actualModalRomanNumerals && quizData.actualModalRomanNumerals.length > 0) {
        quizData.actualModalRomanNumerals.forEach(modal => {
            const span = document.createElement('span');
            span.innerHTML = `${modal.RomanNumerals} (relative to ${modal.RelativeTo})<br />`; // Assuming PascalCase from C# serialization
            modalRomanElement.appendChild(span);
        });
        document.getElementById('quizInfoModalSection').style.display = 'block';
    } else {
        document.getElementById('quizInfoModalSection').style.display = 'none';
    }

    // Show hidden info containers
    document.querySelectorAll('.hidden-info').forEach(el => el.style.display = 'block');

    // Also run checkAnswers to apply colors and populate reveal boxes
    const inputElements = document.querySelectorAll('.chord-guess-input');
    const displayBoxes = document.querySelectorAll('.chord-display-box');
    const chordRevealBoxes = document.querySelectorAll('.chord-reveal-box');

    const actualTonalRomanNumerals = quizData.actualRomanNumerals; // Already an array of strings

    inputElements.forEach((input, index) => {
        const actualTonalRoman = actualTonalRomanNumerals[index];
        const displayBox = displayBoxes[index];
        const revealBox = chordRevealBoxes[index];

        // Ensure coloring happens
        checkAnswers(quizData); // Re-run check to ensure colors are applied

        // Fill in the reveal box with the actual chord name
        revealBox.textContent = actualTonalRoman;
    });
}

// Expose functions globally
window.chordQuiz = {
    checkAnswers: checkAnswers,
    revealAnswers: revealAnswers
};