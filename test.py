

def _2bytes(value):

    r = []
    for each in int.to_bytes(value, 4, "big"):
        r.append(each)
    return r

def _2int(values):

    s = 0
    l = len(values)
    r = l - 1
    for i in range(l):
        s += values[i] << (r - i) * 8
    return s

def byte_add(arr1, arr2):

    values = [v1 + v2 for v1, v2 in zip(arr1, arr2)]
    print(values)
    return _2int(values)


print(True +True)