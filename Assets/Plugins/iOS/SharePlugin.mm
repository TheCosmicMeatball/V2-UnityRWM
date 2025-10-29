#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

extern "C" {
    void _ShareImage(const char* imagePath, const char* message) {
        NSString *path = [NSString stringWithUTF8String:imagePath];
        NSString *msg = [NSString stringWithUTF8String:message];

        NSURL *fileURL = [NSURL fileURLWithPath:path];

        if (![[NSFileManager defaultManager] fileExistsAtPath:path]) {
            NSLog(@"Share failed: File not found at %@", path);
            return;
        }

        UIImage *image = [UIImage imageWithContentsOfFile:path];
        if (!image) {
            NSLog(@"Share failed: Could not load image from %@", path);
            return;
        }

        NSMutableArray *items = [NSMutableArray array];
        [items addObject:image];

        if (msg && [msg length] > 0) {
            [items addObject:msg];
        }

        dispatch_async(dispatch_get_main_queue(), ^{
            UIViewController *rootVC = [[[UIApplication sharedApplication] keyWindow] rootViewController];

            UIActivityViewController *activityVC = [[UIActivityViewController alloc]
                initWithActivityItems:items
                applicationActivities:nil];

            // Exclude activity types that don't make sense for sharing game results
            activityVC.excludedActivityTypes = @[
                UIActivityTypeAssignToContact,
                UIActivityTypePrint,
                UIActivityTypeAddToReadingList
            ];

            // For iPad: configure popover presentation
            if ([activityVC respondsToSelector:@selector(popoverPresentationController)]) {
                activityVC.popoverPresentationController.sourceView = rootVC.view;
                activityVC.popoverPresentationController.sourceRect = CGRectMake(
                    rootVC.view.bounds.size.width / 2,
                    rootVC.view.bounds.size.height / 2,
                    0, 0
                );
                activityVC.popoverPresentationController.permittedArrowDirections = 0;
            }

            [rootVC presentViewController:activityVC animated:YES completion:nil];
        });
    }
}
