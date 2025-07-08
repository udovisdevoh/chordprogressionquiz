// (Contents as provided in the previous turn)
// wwwroot/js/chordDisplay.js

/**
 * Updates the display with the current chord progression details.
 * @param {object} data - An object containing all the display data.
 * @param {string} data.songName - The name of the song.
 * @param {string} data.keysExample - The keys example string.
 * @param {string} data.tonalRomanNumerals - The Roman numerals string for tonal.
 * @param {string} data.tonalRelativeTo - The key/mode the tonal progression is relative to.
 * @param {Array<Object>} data.modalList - List of modal objects with RomanNumerals and RelativeTo.
 * @param {number|null} data.substitutionGroup - The substitution group number.
 * @param {number|null} data.palindromicGroup - The palindromic group number.
 * @param {number|null} data.rotationGroup - The rotation group number.
 * @param {Array<Object>} data.absoluteChords - Array of MIDI chords.
 * @param {Array<Object>} data.stylizedMidiEvents - Array of stylized MIDI events.
 * @param {number} data.currentProgressionIndex - Current index (0-based).
 * @param {number} data.totalProgressionCount - Total number of progressions.
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
            if (modalSectionElement) modalSectionElement.style.display = 'block'; // Show modal section
            data.modalList.forEach(modal => {
                const span = document.createElement('span');
                // Ensure modal properties are accessed correctly (they are strings after Json.Serialize)
                span.innerHTML = `${modal.romanNumerals || ''} (relative to ${modal.relativeTo || ''})<br />`;
                modalListElement.appendChild(span);
            });
        } else {
            if (modalSectionElement) modalSectionElement.style.display = 'none'; // Hide modal section if no modal data
        }
    }


    // Update Optional Groups
    // For nullable numbers, Json.Serialize will convert null to JS null.
    // So check for 'null' literally, or use `data.value !== null && data.value !== undefined`
    if (substitutionGroupElement) {
        const displayValue = data.substitutionGroup !== null ? data.substitutionGroup : 'N/A';
        substitutionGroupElement.textContent = displayValue;
        if (substitutionGroupElement.closest('p')) { // Ensure parent 'p' exists before trying to style
            substitutionGroupElement.closest('p').style.display = data.substitutionGroup !== null ? 'block' : 'none';
        }
    }
    if (palindromicGroupElement) {
        const displayValue = data.palindromicGroup !== null ? data.palindromicGroup : 'N/A';
        palindromicGroupElement.textContent = displayValue;
        if (palindromicGroupElement.closest('p')) {
            palindromicGroupElement.closest('p').style.display = data.palindromicGroup !== null ? 'block' : 'none';
        }
    }
    if (rotationGroupElement) {
        const displayValue = data.rotationGroup !== null ? data.rotationGroup : 'N/A';
        rotationGroupElement.textContent = displayValue;
        if (rotationGroupElement.closest('p')) {
            rotationGroupElement.closest('p').style.display = data.rotationGroup !== null ? 'block' : 'none';
        }
    }


    // Update Base MIDI Pitches
    if (baseMidiPitchesElement) {
        if (data.absoluteChords && data.absoluteChords.length > 0) {
            const section = baseMidiPitchesElement.closest('div[id="baseMidiPitchesSection"]');
            if (section) section.style.display = 'block'; // Show section
            baseMidiPitchesElement.innerHTML = data.absoluteChords.map(chord =>
                `<span>[${chord.midiPitches.join(', ')}]</span>`
            ).join(' | ');
        } else {
            const section = baseMidiPitchesElement.closest('div[id="baseMidiPitchesSection"]');
            if (section) section.style.display = 'none'; // Hide section
        }
    }

    // Update Stylized Playback Details
    if (totalMidiEventsElement) {
        if (data.stylizedMidiEvents && data.stylizedMidiEvents.length > 0) {
            const section = totalMidiEventsElement.closest('div[id="stylizedPlaybackDetailsSection"]');
            if (section) section.style.display = 'block'; // Show section
            totalMidiEventsElement.innerHTML = `Total MIDI Events: ${data.stylizedMidiEvents.length}<br />`;
        } else {
            const section = totalMidiEventsElement.closest('div[id="stylizedPlaybackDetailsSection"]');
            if (section) section.style.display = 'none'; // Hide section
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