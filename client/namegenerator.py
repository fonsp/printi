from uuid import getnode


def getAt(index, words):
    count = len(words)
    total = count*count - count
    index = index % total

    first_i = index // (count - 1)
    second_i = index % (count - 1)
    if second_i >= first_i:
        second_i += 1
    return words[first_i] + words[second_i]


words = ["frog","cafe","coffee","snacks","train","tetris","rubiks","orange","bike","rhythm","boat","dog","dogger","pupper","puppy","meow","cat","cats","tree","fish","sheep","noodles","bach","guitar","smoothie","kale","veggie","scuba","magenta","green","red","blue","yellow","green","banana","go","potato","soup","carrot","rice","oregano","basil","spices","flavour","journey","canoe","newton","klaas","erasmus","einstein","turing","feynman","fons","maxwell","darwin","moire","eisinga","willow","oak","birch","pine","maple","yew","elk","rabbit","wolf","fox","beaver","attenborough","capibara","squid","whale","seal","walrus","dolphin","bear","umbrella","chess","checkers","sudoku","crossword"]

words = [str.upper(w[0])+w[1:] for w in words]

mac = getnode()
#mac = 0x40a36bc21b6f

print(getAt(mac, words))
