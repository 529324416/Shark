for(i = 100; i < 1000; i += 1){
	h = i/100
	t = (i - h * 100)/10
	d = i % 10
	if(i == d * d * d + t * t * t + h * h * h){
		print(i)
	}
}