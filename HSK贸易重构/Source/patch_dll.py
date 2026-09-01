"""Binary-patch ImperialCoinFix.dll: replace 币(U+5E01) with 令(U+4EE4) in string literals.
Both are 2 bytes in UTF-16LE, so the patch is length-preserving — no metadata changes needed.
"""
import struct

dll_path = r"C:\Personal\Project\ratkin-patch\帝国银币货币\Assemblies\ImperialCoinFix.dll"

# UTF-16LE byte patterns
# 币 = U+5E01 = 01 5E, 令 = U+4EE4 = E4 4E
# We target the full string context to avoid false positives

# 帝国银币 = 1D5E FD56 F694 015E  ->  帝国银令
old_silver = b'\x1D\x5E\xFD\x56\xF6\x94\x01\x5E'  # 帝国银币
new_silver = b'\x1D\x5E\xFD\x56\xF6\x94\xE4\x4E'  # 帝国银令

# 帝国金币 = 1D5E FD56 D191 015E  ->  帝国金令
old_gold = b'\x1D\x5E\xFD\x56\xD1\x91\x01\x5E'    # 帝国金币
new_gold = b'\x1D\x5E\xFD\x56\xD1\x91\xE4\x4E'    # 帝国金令

with open(dll_path, 'rb') as f:
    data = f.read()

count_silver = data.count(old_silver)
count_gold = data.count(old_gold)
print(f"Found '帝国银币' occurrences: {count_silver}")
print(f"Found '帝国金币' occurrences: {count_gold}")

if count_silver == 0 and count_gold == 0:
    print("WARNING: No matches found! DLL may already be patched or strings not present.")
    # Let's also check if 令 is already there
    check_silver = b'\x1D\x5E\xFD\x56\xF6\x94\xE4\x4E'
    check_gold = b'\x1D\x5E\xFD\x56\xD1\x91\xE4\x4E'
    if data.count(check_silver) > 0 or data.count(check_gold) > 0:
        print("DLL appears to already be patched with 令. Exiting.")
    else:
        print("ERROR: Cannot find target strings. Aborting.")
    exit(0)

data = data.replace(old_silver, new_silver)
data = data.replace(old_gold, new_gold)

with open(dll_path, 'wb') as f:
    f.write(data)

print(f"Patched {count_silver + count_gold} string occurrences in {dll_path}")
# Verify
with open(dll_path, 'rb') as f:
    data2 = f.read()
assert data2.count(new_silver) == count_silver, "Silver verification failed!"
assert data2.count(new_gold) == count_gold, "Gold verification failed!"
assert data2.count(old_silver) == 0, "Old silver still present!"
assert data2.count(old_gold) == 0, "Old gold still present!"
print("Verification passed — all replacements confirmed.")
