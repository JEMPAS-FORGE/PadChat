"""Bounded local recognition hints; never rewrite a transcript or execute hints."""
import re

BASE_TERMS = ('Party', 'guild', 'raid', 'quest', 'dungeon', 'mana', 'healing',
              'tank', 'Horde', 'Orgrimmar', 'Thunder Bluff', 'Wailing Caverns',
              'the Barrens', 'hearthstone', 'aggro', 'DPS', 'buff', 'resurrection')


def custom_terms(value):
    if not isinstance(value, str) or len(value.encode('utf-8')) > 600:
        raise ValueError('Vocabulary must fit in 600 UTF-8 bytes.')
    if any(ord(c) < 32 or ord(c) == 127 or c in '/;|"\\' for c in value):
        raise ValueError('Use comma-separated words or names, without commands.')
    result, seen, count = [], set(), 0
    for entry in value.split(','):
        term = ' '.join(entry.split())
        if not term:
            continue
        count += 1
        if count > 32:
            raise ValueError('Use at most 32 custom words or names.')
        if len(term.encode('utf-8')) > 60 or not all(c.isalnum() or c in " '-." for c in term):
            raise ValueError('Use words/names up to 60 characters each.')
        if term.casefold() not in seen:
            seen.add(term.casefold()); result.append(term)
        if len(result) > 32:
            raise ValueError('Use at most 32 custom words or names.')
    return result


def recognition_prompt(value=''):
    # Prioritise explicit player hints; keep the conditioning text comfortably
    # below the decoder's prompt window. Do not add command prefixes or examples
    # that might turn ordinary speech into an invitation.
    terms = custom_terms(value) + list(BASE_TERMS)
    output, seen = [], set()
    for term in terms:
        if term.casefold() in seen:
            continue
        # A long glossary made this small decoder repeat ordinary speech in the
        # shipping regression sample. Cap the complete prompt at 125 UTF-8 bytes.
        if len(('World of Warcraft. ' + ', '.join(output + [term]) + '.').encode('utf-8')) > 125:
            break
        output.append(term); seen.add(term.casefold())
    return 'World of Warcraft. ' + ', '.join(output) + '.'
