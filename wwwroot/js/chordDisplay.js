// wwwroot/js/chordDisplay.js

/**
 * Updates the display with the current chord progression details.
 * @param {string} songName - The name of the song.
 * @param {string} keysExample - The keys example string.
 * @param {string} tonalRomanNumerals - The Roman numerals string for tonal.
 * @param {string} tonalRelativeTo - The key/mode the tonal progression is relative to.
 * @param {Array<Object>} modalList - List of modal objects with RomanNumerals and RelativeTo.
 * @param {number|null} substitutionGroup - The substitution group number.
 * @param {number|null} palindromicGroup - The palindromic group number.
 * @param {number|null} rotationGroup - The rotation group number.
 * @param {Array<Object>} absoluteChords - Array of MIDI chords.
 * @param {Array<Object>} stylizedMidiEvents - Array of stylized MIDI events.
 * @param {number} currentProgressionIndex - Current index (0-based).
 * @param {number} totalProgressionCount - Total number of progressions.
 */
function updateChordDisplay(data) {
    const songNameElement = document.getElementById('songName');
    const keysExampleElement = document.getElementById('keysExample');
    const tonalRomanNumeralsElement = document.getElementById('tonalRomanNumerals');
    const tonalRelativeToElement = document.getElementById('tonalRelativeTo');
    const modalSectionElement = document.getElementById('modalSection');
    const modalListElement = document.getElementById('modalList');
    const substitutionGroupElement = document.getElementById('substitutionGroup');
    const palindromicGroupElement = document.getElementById('palindromicGroup');
    const rotationGroupElement = document.getElementById('rotationGroup');
    const baseMidiPitchesElement = document.getElementById('baseMidiPitches');
    const totalMidiEventsElement = document.getElementById('totalMidiEvents');
    const progressionIndexCountElement = document.getElementById('progressionIndexCount');

    if (songNameElement) songNameElement.textContent = data.songName || '';
    if (keysExampleElement) keysExampleElement.textContent = data.keysExample || '';
    if (tonalRomanNumeralsElement) tonalRomanNumeralsElement.textContent = data.tonalRomanNumerals || '';
    if (tonalRelativeToElement) tonalRelativeToElement.textContent = data.tonalRelativeTo || '';

    // Update Modal section
    if (modalListElement) {
        modalListElement.innerHTML = ''; // Clear previous modal entries
        if (data.modalList && data.modalList.length > 0) {
            modalSectionElement.style.display = 'block'; // Show modal section
            data.modalList.forEach(modal => {
                const span = document.createElement('span');
                span.innerHTML = `${modal.romanNumerals} (relative to ${modal.relativeTo})<br />`;
                modalListElement.appendChild(span);
            });
        } else {
            modalSectionElement.style.display = 'none'; // Hide modal section if no modal data
        }
    }


    // Update Optional Groups
    if (substitutionGroupElement) {
        if (data.substitutionGroup !== null) {
            substitutionGroupElement.textContent = data.substitutionGroup;
            substitutionGroupElement.closest('p').style.display = 'block';
        } else {
            substitutionGroupElement.closest('p').style.display = 'none';
        }
    }
    if (palindromicGroupElement) {
        if (data.palindromicGroup !== null) {
            palindromicGroupElement.textContent = data.palindromicGroup;
            palindromicGroupElement.closest('p').style.display = 'block';
        } else {
            palindromicGroupElement.closest('p').style.display = 'none';
        }
    }
    if (rotationGroupElement) {
        if (data.rotationGroup !== null) {
            rotationGroupElement.textContent = data.rotationGroup;
            rotationGroupElement.closest('p').style.display = 'block';
        } else {
            rotationGroupElement.closest('p').style.display = 'none';
        }
    }

    // Update Base MIDI Pitches
    if (baseMidiPitchesElement) {
        if (data.absoluteChords && data.absoluteChords.length > 0) {
            baseMidiPitchesElement.closest('div').style.display = 'block'; // Show section
            baseMidiPitchesElement.innerHTML = data.absoluteChords.map(chord =>
                `<span>[${chord.midiPitches.join(', ')}]</span>`
            ).join(' | ');
        } else {
            baseMidiPitchesElement.closest('div').style.display = 'none'; // Hide section
        }
    }

    // Update Stylized Playback Details
    if (totalMidiEventsElement) {
        if (data.stylizedMidiEvents && data.stylizedMidiEvents.length > 0) {
            totalMidiEventsElement.closest('div').style.display = 'block'; // Show section
            totalMidiEventsElement.innerHTML = `Total MIDI Events: ${data.stylizedMidiEvents.length}<br />`;
        } else {
            totalMidiEventsElement.closest('div').style.display = 'none'; // Hide section
        }
    }

    // Update Progression Index Count
    if (progressionIndexCountElement) {
        progressionIndexCountElement.textContent = `Progression ${data.currentProgressionIndex + 1} of ${data.totalProgressionCount}`;
    }
}

// Expose globally
window.chordDisplay = {
    updateChordDisplay: updateChordDisplay
};