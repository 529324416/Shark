# import random


# def dot(l, r):
#     s = 0
#     for w,v in zip(l, r):
#         s += w * v
#     return s

# def target(values):
#     return values[0] * 3 + values[1] * 4 + 5



# class Perceptron:

#     def __init__(self):

#         self.w = [0, 0]
#         self.b = 0
#         self.lr = 0.0001

#     def train(self, IN, label):
        
#         prediction = dot(IN, self.w) + self.b
#         error = label - prediction
#         self.w[0] += error * self.lr * IN[0]
#         self.w[1] += error * self.lr * IN[1]
#         self.b += error * self.lr

#     def show(self):
#         print(self.w, self.b)

# x = Perceptron()
# for i in range(1000000):
#     value = [random.randint(0, 100), random.randint(0, 100)]
#     x.train(value, target(value))

# x.show()

for i in range(10):
    pass

print(i)