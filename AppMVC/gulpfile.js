var gulp = require('gulp');
var cssmin = require('gulp-cssmin');
var rename = require('gulp-rename');
const sass = require('gulp-sass')(require('sass')); // Đảm bảo rằng 'sass' là tên gói đúng

// Định nghĩa task 'default' để biên dịch SCSS và tối ưu CSS
gulp.task('default', function () {
    return gulp.src('assets/scss/site.scss') // Đọc file SCSS
        .pipe(sass().on('error', sass.logError)) // Biên dịch SCSS và xử lý lỗi
        //.pipe(cssmin()) // Tùy chọn: Nén CSS (bỏ nếu không cần)
        .pipe(rename({ // Đổi tên file đầu ra (nếu cần)
            //suffix: '.min'
        }))
        .pipe(gulp.dest('wwwroot/css/')); // Đầu ra file CSS
});

// Định nghĩa task 'watch' để theo dõi thay đổi trong các file SCSS
gulp.task('watch', function () {
    gulp.watch('assets/scss/*.scss', gulp.series('default')); // Theo dõi và chạy task 'default' khi có thay đổi
});
