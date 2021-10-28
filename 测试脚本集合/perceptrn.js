/* Shark脚本案例：感知机算法 */

function dot(l, r){
    s = 0
    for(i = 0; i < 2; i += 1){
        s += l[i] * r[i]
    }
    return s
}

function Perceptron(){

    this.weights = [0, 0]
    this.bias = 0
    this.lr = 0.0001
    this.train = function(IN, label){

        prediction = dot(IN, this.weights)
        error = label - prediction
        for(i = 0; i < 2; i += 1){
            this.weights[i] += error * IN[i] * this.lr
        }
        this.bias += error * this.lr
    }
    this.output = function(){
        print(this.weights, this.bias)
    }
    return this
}

function t(IN){
    return IN[0] * 3 + IN[1] * 4 + 5
}



x = Perceptron()
for(i = 0; i < 1000000; i += 1){

    IN = [random(), random()]
    x.train(IN, t(IN))
}

x.output()