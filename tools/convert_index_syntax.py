"""One-shot migration: F# old indexing `expr.[idx]` -> modern `expr[idx]`.

Converts code and comments; leaves string literals (regular, verbatim,
triple-quoted) untouched so generated-code text and messages survive.
"""
import os
import re
import io

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def convert(text: str):
    out = []
    i, n = 0, len(text)
    changed = 0
    while i < n:
        c = text[i]
        # line comment
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            j = text.find('\n', i)
            j = n if j == -1 else j
            seg = text[i:j]
            changed += seg.count('.[')
            out.append(seg.replace('.[', '['))
            i = j
            continue
        # block comment (nestable)
        if c == '(' and i + 1 < n and text[i + 1] == '*':
            depth = 1
            j = i + 2
            while j < n and depth:
                if text.startswith('(*', j):
                    depth += 1
                    j += 2
                elif text.startswith('*)', j):
                    depth -= 1
                    j += 2
                else:
                    j += 1
            seg = text[i:j]
            changed += seg.count('.[')
            out.append(seg.replace('.[', '['))
            i = j
            continue
        # triple-quoted string
        if text.startswith('"""', i):
            j = text.find('"""', i + 3)
            j = n if j == -1 else j + 3
            out.append(text[i:j])
            i = j
            continue
        # verbatim string @"..." ("" escape)
        if c == '"' and i > 0 and text[i - 1] == '@':
            j = i + 1
            while j < n:
                if text[j] == '"':
                    if j + 1 < n and text[j + 1] == '"':
                        j += 2
                        continue
                    j += 1
                    break
                j += 1
            out.append(text[i:j])
            i = j
            continue
        # regular string (backslash escapes; interpolated holes untouched)
        if c == '"':
            j = i + 1
            while j < n:
                if text[j] == '\\':
                    j += 2
                    continue
                if text[j] == '"':
                    j += 1
                    break
                j += 1
            out.append(text[i:j])
            i = j
            continue
        # char literal ('\\', 'a'); a generic tick like 'T does not match
        m = re.match(r"'(\\.|[^\\'])'", text[i:i + 6])
        if c == "'" and m:
            out.append(m.group(0))
            i += len(m.group(0))
            continue
        # code: the migration itself
        if c == '.' and i + 1 < n and text[i + 1] == '[':
            out.append('[')
            changed += 1
            i += 2
            continue
        out.append(c)
        i += 1
    return ''.join(out), changed


def main():
    targets = []
    for base in ('src', 'tests', 'games', 'tools'):
        for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, base)):
            parts = dirpath.replace('\\', '/').split('/')
            if 'bin' in parts or 'obj' in parts:
                continue
            for f in filenames:
                if f.endswith(('.fs', '.fsx', '.fsi')):
                    targets.append(os.path.join(dirpath, f))
    total = 0
    touched = 0
    for path in targets:
        with io.open(path, encoding='utf-8-sig') as fh:
            text = fh.read()
        new, changed = convert(text)
        if changed:
            with io.open(path, 'w', encoding='utf-8', newline='') as fh:
                fh.write(new)
            total += changed
            touched += 1
            print(f'{changed:5d}  {path}')
    print(f'total occurrences converted: {total} in {touched} files')


if __name__ == '__main__':
    main()
