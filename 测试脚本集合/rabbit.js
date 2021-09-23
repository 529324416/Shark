
function Rabbit(){

    this.food = 11
    this.energy = 2


    this.isHungry = function(){
        return this.energy < 15
    }
    this.eat = function(){

        value = 10
        if(this.food >= 10){
            this.food -= 10
            this.energy += 10
        }else if(this.food > 0){

            value = this.food
            this.food = 0
            print("食物的数量不足10", value, this.food)
            this.energy += value
        }else{
            return
        }
        print("小兔子吃了", value, "点食物")
        print("剩余的食物数量为: ", this.food)
    }
    this.play = function(){

        value = randint(2, 6)
        if(dice(0.5)){
            print("小兔子正在睡觉, 消耗了", value, "点能量")
        }else{
            print("小兔子正在玩耍, 消耗了", value, "点能量")
        }
        this.energy -= value
    }
    this.findFood = function(){

        this.energy -= 1
        print("小兔子正在找食物")
        if(dice(0.7)){
            value = randint(5, 15)
            print("小兔子找到了",value,"点食物")
            this.food += value
        }else{
            printr("小兔子没有找到食物")
        }
    }
    this.main = function(){

        while(true){
            sleep(1)
            if(this.isHungry()){
                if(this.food > 0){
                    this.eat()
                }else{
                    this.findFood()
                }
            }else{
                this.play()
            }
        }
    }
    return this
}

x = Rabbit()
x.main()


